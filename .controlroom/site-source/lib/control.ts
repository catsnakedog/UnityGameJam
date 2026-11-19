import {z} from 'zod';
import {State,Task,Report,Contract,reportSchema,rid,risks,handoff,executionSchema,delivered} from './model';
import planExample from './control-plan-example.json';

export type Actor={id:string;name:string};
export type Connection={id:string;userId:string;name:string;status:'pending'|'approved'|'revoked';at:string};
export type Claim={taskId:string;connectionId:string;userId:string;startedAt:string;snapshot:Task};
export type PlanSubmission={id:string;connectionId:string;name:string;at:string;status:'pending'|'approved'|'rejected';feedback:string;plan:Plan};
export type Receipt={userId:string;requestId:string;fingerprint:string;result:Record<string,unknown>};
export type Control={connections:Connection[];claims:Claim[];plans:PlanSubmission[];receipts:Receipt[];autoRegisterPlans:boolean};
export type Workspace=State&{control?:Control};
const text=z.string().trim().min(1).max(12000),id=z.string().regex(/^[a-zA-Z0-9][a-zA-Z0-9_.:-]{0,119}$/);
const refs=z.array(id).max(200).default([]);
const contractSchema=z.object({id,name:text,kind:z.enum(['interface','data','event','api','other']),purpose:text,usage:text,public_structure:text,definition_path:text,owner_task:id.nullable(),existing_owner:text.optional(),evidence:text.optional()});
export const planSchema=z.object({
 project:z.object({name:text,engine:z.literal('Unity').default('Unity'),overview:z.string().max(20000).optional()}).passthrough(),
 requirements:z.array(z.object({id,text,source:text}).passthrough()).max(500),
 epics:z.array(z.object({id,title:text})).max(100),
 tasks:z.array(z.object({id,execution:executionSchema.optional(),epic_id:id,parent_id:id.nullable().default(null),title:z.string().min(1).max(180),implementation:text,completion_criteria:z.array(text).min(1).max(100),requirement_ids:refs,depends_on:refs,related_to:refs,integrates:refs,produces:refs,consumes:refs,modifies:z.array(z.object({path:text,basis:z.enum(['observed','proposed']),reason:text})).max(100).default([])}).passthrough()).min(1).max(100),
 contracts:z.array(contractSchema).max(200).default([])
});
export type Plan=z.infer<typeof planSchema>;
export class ControlError extends Error{constructor(public code:string,message:string,public status=400,public details?:unknown){super(message)}}
export function ensure(ok:unknown,code:string,message:string,status=400,details?:unknown):asserts ok{if(!ok)throw new ControlError(code,message,status,details)}
export function normalize(input:State):Workspace{const s=structuredClone(input) as Workspace;s.control={connections:[],claims:[],plans:[],receipts:[],autoRegisterPlans:false,...s.control};return s}
export function control(s:Workspace):Control{return s.control!}
const now=()=>new Date().toISOString();
function log(s:Workspace,actor:Actor,title:string,detail:string,taskId?:string){s.history.unshift({id:rid(),title,detail,taskId,actor:actor.name,at:now()})}
function task(s:Workspace,id:string){const t=s.tasks.find(t=>t.id===id);ensure(t,'task_missing','작업을 찾을 수 없습니다.',404);return t}
function connection(s:Workspace,actor:Actor,cid:string){const c=control(s).connections.find(c=>c.id===cid&&c.userId===actor.id);ensure(c?.status==='approved','connection_required','관제탑의 스킬 연결 화면에서 최초 연결을 승인해 주세요.',403);return c}
function unique(values:string[],label:string){ensure(new Set(values).size===values.length,'duplicate',`${label} ID가 중복됩니다.`)}
function validatePlan(s:Workspace,p:Plan){
 unique(p.tasks.map(t=>t.id),'작업');unique(p.requirements.map(r=>r.id),'요구사항');unique(p.epics.map(e=>e.id),'Epic');unique(p.contracts.map(c=>c.id),'계약');
 const allTasks=[...s.tasks.map(t=>t.id),...p.tasks.map(t=>t.id)],reqs=[...s.requirements.map(r=>r.id),...p.requirements.map(r=>r.id)],contracts=[...s.contracts.map(c=>c.id),...p.contracts.map(c=>c.id)];
 unique(allTasks,'기존/새 작업');unique(reqs,'기존/새 요구사항');
 for(const t of p.tasks){
  ensure(p.epics.some(e=>e.id===t.epic_id),'epic_missing',`${t.id}: Epic 확인 필요`);
  ensure(!t.parent_id||allTasks.includes(t.parent_id),'parent_missing',`${t.id}: 상위 작업 확인 필요`);
  ensure(t.depends_on.every(i=>allTasks.includes(i))&&t.related_to.every(i=>allTasks.includes(i))&&t.integrates.every(i=>allTasks.includes(i)),'dependency_missing',`${t.id}: 작업 관계 확인 필요`);
  ensure(t.requirement_ids.every(i=>reqs.includes(i)),'requirement_missing',`${t.id}: 요구사항 확인 필요`);
  ensure([...t.produces,...t.consumes].every(i=>contracts.includes(i)),'contract_missing',`${t.id}: 공용 계약 확인 필요`);
  ensure(t.modifies.every(m=>!m.path.startsWith('/')&&!m.path.includes('..')&&!m.path.includes(':')),'path_invalid','프로젝트 상대 경로를 사용하세요.');
 }
 for(const field of ['depends_on','parent_id'] as const){
  const visit=(tid:string,chain:Set<string>):void=>{ensure(!chain.has(tid),'cycle',`${field}: 순환 관계가 있습니다.`);const t=p.tasks.find(t=>t.id===tid);if(!t)return;const next=new Set([...chain,tid]);for(const dep of field==='parent_id'?(t.parent_id?[t.parent_id]:[]):t.depends_on)visit(dep,next)};
  for(const t of p.tasks)visit(t.id,new Set());
 }
 for(const c of p.contracts){const existing=s.contracts.find(v=>v.id===c.id);if(existing){ensure(c.owner_task===null&&!!c.existing_owner&&!!c.evidence,'contract_exists','기존 계약은 소비 참조로 등록하고 변경은 계약 수정으로 제출하세요.');continue}ensure(c.owner_task&&p.tasks.some(t=>t.id===c.owner_task),'contract_owner','새 계약의 소유 작업이 필요합니다.');const producers=p.tasks.filter(t=>t.produces.includes(c.id));ensure(producers.length===1&&producers[0].id===c.owner_task,'contract_owner','계약 생산자는 소유 작업 하나여야 합니다.')}
}
function registerPlan(s:Workspace,submission:PlanSubmission,actor:Actor){
 const p=submission.plan;validatePlan(s,p);
 s.requirements.push(...p.requirements);
 for(const t of p.tasks)s.tasks.push({id:t.id,title:t.title,description:t.implementation,group:p.epics.find(e=>e.id===t.epic_id)!.title,phase:'',owner:'',status:'대기',approval:'확인 필요',scope:t.modifies.map(m=>({path:m.path,region:'전체'})),interfaces:[],contracts:[...new Set([...t.produces,...t.consumes])],depends:t.depends_on,requirements:t.requirement_ids,criteria:t.completion_criteria,version:1,ackVersion:0,kind:t.integrates.length?'통합':'개발',related:[...new Set([...t.related_to,...t.integrates])],archive:false,parentId:t.parent_id,planId:submission.id,execution:t.execution,planningNotes:JSON.stringify({modifies:t.modifies,assumptions:t.assumptions,open_questions:t.open_questions,risks:t.risks,exclusions:t.exclusions},null,2)});
 for(const c of p.contracts)if(!s.contracts.some(v=>v.id===c.id))s.contracts.push({id:c.id,name:c.name,schema:c.public_structure,version:1,confirmed:false,owner:c.owner_task||c.existing_owner||'',kind:c.kind,purpose:c.purpose,usage:c.usage,definitionPath:c.definition_path,ownerTask:c.owner_task||undefined,updatedAt:now()});
 submission.status='approved';log(s,actor,'작업 계획 등록',`${submission.id} · ${p.tasks.length}개 작업 · 원본 계획 보존`);
}
export const writeSchema=z.discriminatedUnion('operation',[
 z.object({operation:z.literal('join'),connection_id:id,request_id:id,payload:z.object({name:z.string().trim().min(1).max(80)})}),
 z.object({operation:z.literal('plan_submit'),connection_id:id,request_id:id,payload:z.object({plan:planSchema,registration:z.enum(['review','automatic']).default('review')})}),
 z.object({operation:z.literal('claim'),connection_id:id,request_id:id,payload:z.object({task_ids:z.array(id).min(1).max(100),owner:z.string().trim().min(1).max(80)})}),
 z.object({operation:z.literal('release'),connection_id:id,request_id:id,payload:z.object({task_ids:z.array(id).min(1).max(100)})}),
 z.object({operation:z.literal('complete'),connection_id:id,request_id:id,payload:z.object({reports:z.array(z.object({report:reportSchema,latest_version:z.number().int().positive(),finish_mode:z.enum(['latest','started'])})).min(1).max(100)})}),
 z.object({operation:z.literal('contract_upsert'),connection_id:id,request_id:id,payload:z.object({expected_revision:z.number().int().nonnegative(),contract:contractSchema,deleted:z.boolean().default(false)})})
]);
export function writeOperation(input:State,raw:unknown,actor:Actor){
 const a=writeSchema.parse(raw),s=normalize(input),ctl=control(s);let result:Record<string,unknown>={ok:true};
 if(a.operation==='join'){
  const existing=ctl.connections.find(c=>c.id===a.connection_id);ensure(!existing||existing.userId===actor.id,'connection_conflict','다른 사용자의 연결 ID입니다.',409);
  if(!existing){const approved=ctl.connections.some(c=>c.userId===actor.id&&c.status==='approved');ctl.connections.push({id:a.connection_id,userId:actor.id,name:a.payload.name,status:approved?'approved':'pending',at:now()});log(s,actor,'스킬 연결 요청',a.payload.name)}
  result={ok:true,connection:ctl.connections.find(c=>c.id===a.connection_id)};
 }else{
  const agent=connection(s,actor,a.connection_id);
  if(a.operation==='plan_submit'){
   validatePlan(s,a.payload.plan);const submission:PlanSubmission={id:rid(),connectionId:agent.id,name:a.payload.plan.project.name,at:now(),status:'pending',feedback:'',plan:a.payload.plan};
   ctl.plans.push(submission);if(a.payload.registration==='automatic'||ctl.autoRegisterPlans&&ctl.plans.some(p=>p.status==='approved'))registerPlan(s,submission,actor);else log(s,actor,'작업 계획 검토 요청',`${submission.plan.tasks.length}개 작업`);
   result={ok:true,plan_id:submission.id,status:submission.status,task_ids:submission.plan.tasks.map(t=>t.id)};
  }
  if(a.operation==='claim'){
   unique(a.payload.task_ids,'선택 작업');const selected=a.payload.task_ids.map(tid=>task(s,tid));
   for(const t of selected){const claim=ctl.claims.find(c=>c.taskId===t.id);ensure(!claim,'claimed',`${t.id}: 이미 선점되어 있습니다.`,409);ensure(!t.archive&&['대기','재작업'].includes(t.status),'task_state',`${t.id}: 선점 가능한 상태가 아닙니다.`,409);ensure(!t.owner||t.owner===a.payload.owner,'assigned',`${t.id}: 지정 담당자가 다릅니다.`,409)}
   if(!s.members.includes(a.payload.owner))s.members.push(a.payload.owner);
   for(const t of selected){t.owner=a.payload.owner;t.status='진행 중';t.ackVersion=t.version;ctl.claims.push({taskId:t.id,userId:actor.id,connectionId:agent.id,startedAt:now(),snapshot:structuredClone(t)});log(s,actor,'작업 선점',`${a.payload.owner} · ${agent.name}`,t.id)}
   result={ok:true,tasks:selected,contracts:s.contracts.filter(c=>!c.deleted),warnings:risks(s).filter(r=>selected.some(t=>r.taskIds.includes(t.id)))};
  }
  if(a.operation==='release'){
   unique(a.payload.task_ids,'선택 작업');for(const tid of a.payload.task_ids){const c=ctl.claims.find(c=>c.taskId===tid);ensure(c&&c.userId===actor.id&&c.connectionId===agent.id,'not_owner',`${tid}: 현재 연결의 선점이 아닙니다.`,403);const t=task(s,tid);ensure(t.status!=='완료','task_state','완료 작업은 해제할 수 없습니다.');t.owner='';t.status='대기';log(s,actor,'선점 해제','로컬 파일은 변경하지 않습니다.',tid)}
   ctl.claims=ctl.claims.filter(c=>!a.payload.task_ids.includes(c.taskId));result={ok:true,released:a.payload.task_ids};
  }
  if(a.operation==='complete'){
   unique(a.payload.reports.map(r=>r.report.taskId),'보고 작업');
   for(const item of a.payload.reports){const t=task(s,item.report.taskId),claim=ctl.claims.find(c=>c.taskId===t.id);ensure(claim&&claim.userId===actor.id&&claim.connectionId===agent.id,'not_owner',`${t.id}: 현재 연결의 선점이 아닙니다.`,403);ensure(['진행 중','재작업'].includes(t.status),'task_state','진행 중인 작업만 제출할 수 있습니다.',409);ensure(t.version===item.latest_version,'spec_changed','명세가 다시 변경되었습니다. 최신 내용을 확인하세요.',409,{task_id:t.id,latest_version:t.version,started_version:claim.snapshot.version});const basis=item.finish_mode==='started'?claim.snapshot:t;ensure(item.report.version===basis.version,'basis_mismatch','선택한 종료 기준과 보고서 버전이 다릅니다.',409);ensure(basis.requirements.every(id=>item.report.coverage.some(c=>c.id===id))&&basis.criteria.every(text=>item.report.criteria.some(c=>c.text===text)),'coverage_missing','기준 명세의 요구사항과 완료 조건별 상태를 모두 기록하세요.');s.reports.push({...item.report,id:rid(),at:now(),latestVersion:t.version,finishMode:item.finish_mode});t.status='검수 대기';t.approval='승인 대기';t.ackVersion=basis.version;log(s,actor,'완료 보고 제출',`기준 v${basis.version} / 최신 v${t.version} · ${item.finish_mode} · 실제 검증 여부는 보고서 참고`,t.id)}
   ctl.claims=ctl.claims.filter(c=>!a.payload.reports.some(r=>r.report.taskId===c.taskId));result={ok:true,submitted:a.payload.reports.map(r=>r.report.taskId),status:'검수 대기',approved:false};
  }
  if(a.operation==='contract_upsert'){
   const p=a.payload,c=p.contract,old=s.contracts.find(v=>v.id===c.id);ensure((old?.version??0)===p.expected_revision,'revision_conflict','공용 계약이 변경되었습니다. 최신 revision을 확인하세요.',409,{current_revision:old?.version??0});const ownerTask=old?.ownerTask||c.owner_task;ensure(ownerTask&&ctl.claims.some(cl=>cl.taskId===ownerTask&&cl.userId===actor.id&&cl.connectionId===agent.id),'contract_owner','계약 소유 작업을 선점한 연결만 수정할 수 있습니다.',403);ensure(!old?.ownerTask||c.owner_task===old.ownerTask,'contract_owner','소유 Task 변경은 웹에서 조정하세요.',409);const next:Contract={id:c.id,name:c.name,schema:c.public_structure,version:(old?.version??0)+1,confirmed:false,owner:ownerTask,ownerTask,kind:c.kind,purpose:c.purpose,usage:c.usage,definitionPath:c.definition_path,updatedAt:now(),deleted:p.deleted};s.contracts=s.contracts.filter(v=>v.id!==c.id);s.contracts.push(next);log(s,actor,p.deleted?'공용 계약 삭제 표시':'공용 계약 공유',JSON.stringify({previous:old??null,current:next}));result={ok:true,contract:next};
  }
 }
 return {state:s,result};
}
export function readOperation(input:State,raw:unknown,actor:Actor){
 const a=z.object({operation:z.enum(['context','protocol','tasks','task','contract_manifest','contract_detail','request_status']),plan_id:id.optional(),task_id:id.optional(),contract_id:id.optional(),request_id:id.optional(),connection_id:id.optional()}).parse(raw),s=normalize(input),ctl=control(s);
 if(a.operation==='protocol')return {protocol_version:3,project_id:'main',engine:'Unity',transport:'Sites MCP: crdd_read / crdd_write',plan_example:planExample,plan_format:{required:['project','requirements','epics','tasks','contracts'],execution:{lane:['contract','code','data','ui','integration'],skills:'skill names',resources:'exclusive local resources'},registration:{review:'web review',automatic:'registration explicitly authorized by user or calling workflow'}},write_envelope:{operation:'join',connection_id:'persistent-local-UUID',request_id:'unique-per-mutation-reuse-on-retry',payload:{name:'개발자'}},payloads:{join:{name:'개발자'},plan_submit:{plan:'plan_example',registration:'review | automatic'},claim:{task_ids:['TASK-ID'],owner:'개발자'},release:{task_ids:['TASK-ID']},complete:{reports:[{latest_version:1,finish_mode:'latest',report:{taskId:'TASK-ID',version:1,coverage:[{id:'REQ-ID',status:'구현 | 부분 구현 | 미구현 | 확인 필요',note:'근거'}],criteria:[{text:'완료 조건',status:'충족 | 미충족 | 확인 필요'}],behavior:'실제 기능',difference:'차이',missing:'미구현',links:'파일/계약',next:'다음 단계',evidence:'실제 검증'}}]},contract_upsert:{expected_revision:0,contract:planExample.contracts[0],deleted:false}},instructions:['최초 join은 웹 연결 승인 필요; 이후 같은 사용자의 연결 자동 승인','위임된 범위의 계획은 registration=automatic으로 등록 가능','claim/complete 배치는 원자적; 실행 순서와 구현은 호출자 담당','서버 선점은 파일 잠금이 아님; 파일·Editor 조정은 로컬 담당','complete는 검수 대기이며 검수 승인이 아님'],limits:{tasks_per_batch:100,request_characters:800000}};
 if(a.operation==='context')return {project_id:'main',name:s.name,plan:s.plan,requirements:s.requirements,shared:s.shared,actor,connections:ctl.connections.filter(c=>c.userId===actor.id),policies:{engine:'Unity',task_selection:'user or authorized calling workflow',plan_approval:'review or explicitly authorized automatic registration',heartbeat:false},protocol:'crdd_read operation=protocol'};
 if(a.operation==='tasks')return {tasks:s.tasks.filter(t=>!a.plan_id||t.planId===a.plan_id).map(t=>({id:t.id,title:t.title,group:t.group,parent_id:t.parentId??null,status:t.status,owner:t.owner,version:t.version,archive:t.archive,depends:t.depends,scope:t.scope,execution:t.execution,plan_id:t.planId,delivered:delivered(s,t)})),pending_plans:ctl.plans.map(p=>({id:p.id,name:p.name,status:p.status,task_count:p.plan.tasks.length}))};
 if(a.operation==='task'){ensure(a.task_id,'id_required','task_id가 필요합니다.');const t=task(s,a.task_id);const claim=ctl.claims.find(c=>c.taskId===t.id);return {task:t,started_spec:claim?.snapshot??null,claim:claim?{connection_id:claim.connectionId,owned_by_current_user:claim.userId===actor.id}:null,requirements:s.requirements.filter(r=>t.requirements.includes(r.id)),contracts:s.contracts.filter(c=>t.contracts.includes(c.id)),reports:s.reports.filter(r=>r.taskId===t.id),warnings:risks(s).filter(r=>r.taskIds.includes(t.id)),markdown:handoff(s,t)}}
 if(a.operation==='contract_manifest')return {complete:true,items:s.contracts.map(c=>({id:c.id,revision:c.version,updated_at:c.updatedAt||'legacy',deleted:!!c.deleted}))};
 if(a.operation==='contract_detail'){const c=s.contracts.find(c=>c.id===a.contract_id);ensure(c,'contract_missing','계약을 찾을 수 없습니다.',404);return {...c,revision:c.version,updated_at:c.updatedAt||'legacy'}}
 const receipt=ctl.receipts.find(r=>r.userId===actor.id&&r.requestId===a.request_id);return receipt?{found:true,result:receipt.result}:{found:false};
}
export function webControl(input:State,raw:unknown,actor:Actor){
 const a=z.discriminatedUnion('type',[
  z.object({type:z.literal('connection_review'),id,approve:z.boolean()}),
  z.object({type:z.literal('plan_review'),id,approve:z.boolean(),feedback:text}),
  z.object({type:z.literal('control_settings'),autoRegisterPlans:z.boolean()}),
  z.object({type:z.literal('plan_propose'),plan:planSchema,registration:z.enum(['review','automatic']).default('review')}),
 ]).parse(raw),s=normalize(input),ctl=control(s);
 if(a.type==='connection_review'){const c=ctl.connections.find(c=>c.id===a.id);ensure(c,'missing','연결을 찾을 수 없습니다.');ensure(c.userId===actor.id,'not_owner','자신의 연결만 승인하거나 해제할 수 있습니다.',403);c.status=a.approve?'approved':'revoked';log(s,actor,a.approve?'스킬 연결 승인':'스킬 연결 해제',c.name)}
 if(a.type==='plan_review'){const p=ctl.plans.find(p=>p.id===a.id);ensure(p&&p.status==='pending','plan_state','검토 대기 계획을 찾을 수 없습니다.',409);p.feedback=a.feedback;if(a.approve)registerPlan(s,p,actor);else{p.status='rejected';log(s,actor,'작업 계획 반려',`${p.id} · ${a.feedback}`)}}
 if(a.type==='control_settings'){ensure(!a.autoRegisterPlans||ctl.plans.some(p=>p.status==='approved'),'first_review','최초 계획을 승인한 뒤 자동 등록을 설정할 수 있습니다.');ctl.autoRegisterPlans=a.autoRegisterPlans;log(s,actor,'후속 계획 등록 설정',a.autoRegisterPlans?'최초 승인 이후 자동 등록':'웹 검토 후 등록')}
 if(a.type==='plan_propose'){validatePlan(s,a.plan);const p:PlanSubmission={id:rid(),connectionId:'web:'+actor.id,name:a.plan.project.name,at:now(),status:'pending',feedback:'',plan:a.plan};ctl.plans.push(p);if(a.registration==='automatic')registerPlan(s,p,actor);else log(s,actor,'작업 계획 검토 요청',`${a.plan.tasks.length}개 작업 · 파일/붙여넣기`)}
 return s;
}
