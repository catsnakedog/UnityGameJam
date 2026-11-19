import {test} from 'node:test';
import assert from 'node:assert/strict';
import {loadState,saveState,mutateControl,readControl} from '../lib/control-service';
import {emptyState} from '../lib/model';
import {webControl,control,normalize,writeOperation,readOperation} from '../lib/control';
import {POST as mcp} from '../app/mcp/route';
import {GET as apiGet} from '../app/api/control/route';
import example from '../lib/control-plan-example.json';
const actor={id:'agent-user',name:'개발자'};
const envelope=(operation:string,connection_id:string,payload:unknown,request_id=crypto.randomUUID())=>({operation,connection_id,payload,request_id});
async function web(action:unknown){const x=await loadState(actor);const next=webControl(x.state,action,actor);assert.ok(await saveState(next,x.revision,x.exists))}
test('authenticated tools: approval, plan registration, atomic exclusive claims, idempotency, contracts, changed-spec completion and history',async()=>{
 let initial=await loadState(actor);assert.ok(await saveState(emptyState('Integration fixture',actor.id),initial.revision,initial.exists));
 const join=envelope('join','conn-a',{name:'개발자 PC'});const joined=await mutateControl(join,actor);assert.equal((joined as any).connection.status,'pending');
 await assert.rejects(()=>mutateControl(envelope('claim','conn-a',{task_ids:['TASK-SHOP-SERVICE'],owner:'개발자'}),actor),/승인/);
 await web({type:'connection_review',id:'conn-a',approve:true});
 await mutateControl(envelope('join','conn-b',{name:'다른 PC'}),actor);
 const plan=await mutateControl(envelope('plan_submit','conn-a',{plan:example}),actor);assert.equal((plan as any).status,'pending');assert.equal((await loadState(actor)).state.tasks.length,0);
 await web({type:'plan_review',id:(plan as any).plan_id,approve:true,feedback:'계약과 수정 범위를 검토함'});
 assert.equal((await loadState(actor)).state.tasks.length,2);
 const a=envelope('claim','conn-a',{task_ids:['TASK-SHOP-SERVICE'],owner:'개발자'}),b=envelope('claim','conn-b',{task_ids:['TASK-SHOP-SERVICE'],owner:'개발자'});
 const claims=await Promise.allSettled([mutateControl(a,actor),mutateControl(b,actor)]);assert.equal(claims.filter(r=>r.status==='fulfilled').length,1);
 const winning=claims[0].status==='fulfilled'?a:b,losing=winning===a?b:a,cid=winning.connection_id;
 const retry=await mutateControl(winning,actor);assert.equal((retry as any).replayed,true);
 await assert.rejects(()=>mutateControl({...winning,payload:{task_ids:['TASK-SHOP-UI'],owner:'개발자'}},actor),/같은 request_id/);
 await assert.rejects(()=>mutateControl(envelope('claim',losing.connection_id,{task_ids:['TASK-SHOP-UI','TASK-SHOP-SERVICE'],owner:'개발자'}),actor),/선점/);
 assert.equal(control((await loadState(actor)).state).claims.length,1);
 await assert.rejects(()=>mutateControl(envelope('release',losing.connection_id,{task_ids:['TASK-SHOP-SERVICE']}),actor),/현재 연결/);
 const contract={...example.contracts[0],kind:'interface'};
 const updated=await mutateControl(envelope('contract_upsert',cid,{expected_revision:1,contract,deleted:false}),actor);assert.equal((updated as any).contract.version,2);
 await assert.rejects(()=>mutateControl(envelope('contract_upsert',cid,{expected_revision:1,contract}),actor),/revision/);
 const manifest=await readControl({operation:'contract_manifest'},actor);assert.equal((manifest as any).items[0].revision,2);
 await mutateControl(envelope('claim',cid,{task_ids:['TASK-SHOP-UI'],owner:'개발자'}),actor);
 let loaded=await loadState(actor);const service=loaded.state.tasks[0],ui=loaded.state.tasks[1];
 const report=(t:typeof service)=>({taskId:t.id,version:t.version,coverage:t.requirements.map(id=>({id,status:'구현',note:'정적 구현 확인; Unity 미실시'})),criteria:t.criteria.map(text=>({text,status:'확인 필요'})),behavior:'조회·구매 동작 구현',difference:'없음',missing:'없음',links:'Assets/Scripts/Shop',next:'사람의 게임 확인',evidence:'Unity 미실시'});
 const firstReport=report(service);
 await assert.rejects(()=>mutateControl(envelope('complete',cid,{reports:[{report:firstReport,latest_version:1,finish_mode:'latest'},{report:{...report(ui),taskId:'TASK-NOT-FOUND'},latest_version:1,finish_mode:'latest'}]}),actor));
 assert.equal((await loadState(actor)).state.reports.length,0);
 loaded=await loadState(actor);loaded.state.tasks[0].version=2;loaded.state.tasks[0].criteria.push('추가 기획 조건');assert.ok(await saveState(loaded.state,loaded.revision,loaded.exists));
 await assert.rejects(()=>mutateControl(envelope('complete',cid,{reports:[{report:firstReport,latest_version:1,finish_mode:'started'}]}),actor),/명세가 다시/);
 const finish=envelope('complete',cid,{reports:[{report:firstReport,latest_version:2,finish_mode:'started'},{report:report(ui),latest_version:1,finish_mode:'latest'}]});
 const finished=await mutateControl(finish,actor);assert.equal((finished as any).status,'검수 대기');assert.equal((finished as any).approved,false);
 loaded=await loadState(actor);assert.equal(loaded.state.reports.length,2);assert.equal(loaded.state.reports[0].version,1);assert.equal(loaded.state.reports[0].latestVersion,2);assert.equal(control(loaded.state).claims.length,0);assert.ok(loaded.state.history.length>5);
 await mutateControl(finish,actor);assert.equal((await loadState(actor)).state.reports.length,2);
 const status=await readControl({operation:'request_status',request_id:finish.request_id},actor);assert.equal((status as any).found,true);
 const other=await readControl({operation:'request_status',request_id:finish.request_id},{id:'other',name:'다른 사람'});assert.equal((other as any).found,false);
});
test('MCP discovery/list, anonymous tool denial and actual authenticated protocol lookup',async()=>{
 const req=(method:string,params:unknown={},auth=true)=>new Request('https://orca.example/mcp',{method:'POST',headers:{'content-type':'application/json',...(auth?{'oai-authenticated-user-id':actor.id}:{})},body:JSON.stringify({jsonrpc:'2.0',id:1,method,params})});
 const discovery:any=await(await mcp(req('server/discover'))).json();assert.deepEqual(discovery.result.supportedVersions,['2026-07-28']);
 const list:any=await(await mcp(req('tools/list'))).json();assert.deepEqual(list.result.tools.map((t:any)=>t.name),['crdd_read','crdd_write']);
 const denied:any=await(await mcp(req('tools/call',{name:'orca_read',arguments:{operation:'context'}},false))).json();assert.equal(denied.result.isError,true);
 const protocol:any=await(await mcp(req('tools/call',{name:'orca_read',arguments:{operation:'protocol'}}))).json();assert.equal(protocol.result.isError,false);assert.equal(protocol.result.structuredContent.plan_example.tasks.length,2);
 assert.equal((await apiGet(new Request('https://orca.example/api/control'))).status,401);
});
test('normalization preserves old workspace and unknown persisted fields',()=>{const old=emptyState('Existing','owner');const extra={...old,customMetadata:{keep:true}};const s=normalize(extra);assert.equal(s.name,'Existing');assert.deepEqual((s as any).customMetadata,{keep:true});assert.deepEqual(control(s).claims,[])});


test('generic automatic registration preserves all requirements and accepts a normal plan without workflow metadata',()=>{
 let s=normalize(emptyState('General plan',actor.id));
 const write=(operation:string,payload:unknown)=>{const r=writeOperation(s,envelope(operation,'coordinator',payload),actor);s=r.state;return r.result as any};
 write('join',{name:'Coordinator'});s=webControl(s,{type:'connection_review',id:'coordinator',approve:true},actor);
 const plan=write('plan_submit',{plan:example,registration:'automatic'});assert.equal(plan.status,'approved');assert.equal(s.tasks.length,example.tasks.length);assert.equal(s.requirements.length,example.requirements.length);
 for(const t of [...s.tasks]){write('claim',{task_ids:[t.id],owner:'Coordinator'});write('complete',{reports:[{latest_version:t.version,finish_mode:'latest',report:{taskId:t.id,version:t.version,coverage:t.requirements.map(id=>({id,status:'구현',note:'fixture'})),criteria:t.criteria.map(text=>({text,status:'충족'})),behavior:'fixture',difference:'none',missing:'none',links:'fixture',next:'review',evidence:'fixture'}}]})}
 const listed=()=>((readOperation(s,{operation:'tasks',plan_id:plan.plan_id},actor) as any).tasks);
 assert.ok(listed().every((t:any)=>t.delivered));s.tasks[0].version++;assert.equal(listed()[0].delivered,false);
 const protocol=readOperation(s,{operation:'protocol'},actor) as any;assert.equal(protocol.protocol_version,3);assert.deepEqual(Object.keys(protocol.payloads).sort(),['claim','complete','contract_upsert','join','plan_submit','release']);
});
test('browser immediate registration preserves existing tasks, reports and opaque historical extension data',()=>{
 const old=normalize(emptyState('Existing',actor.id));old.history.push({id:'keep',title:'Keep',detail:'Keep',actor:'Old',at:'2020-01-01'});
 (old.control as any).runs=[{id:'legacy',reports:[{notes:'preserve'}]}];
 const next=webControl(old,{type:'plan_propose',plan:example,registration:'automatic'},actor);
 assert.equal(next.tasks.length,example.tasks.length);assert.ok(next.history.some(h=>h.id==='keep'));assert.deepEqual((next.control as any).runs,(old.control as any).runs);
 assert.throws(()=>webControl(next,{type:'plan_propose',plan:example,registration:'automatic'},actor),/중복/);
});
