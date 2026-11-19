import {test} from 'node:test';
import assert from 'node:assert/strict';
import {GET,POST} from '../app/api/workspace/route';
const url='https://control.example/api/workspace';
function req(action:any,revision:number,actor='planner'){return new Request(url,{method:'POST',headers:{'Content-Type':'application/json',...(actor?{'oai-authenticated-user-id':actor,'oai-authenticated-user-full-name':'Planner'}:{})},body:JSON.stringify({action,revision})})}
test('route persists state, rejects anonymous writes, and safely rejects concurrent revisions',async()=>{
 const data:any=await (await GET(new Request(url,{headers:{'oai-authenticated-user-id':'planner'}}))).json();assert.equal(data.revision,0);
 const action={type:'settings',name:'Persistent project',members:['Planner']};
 assert.equal((await POST(req(action,0,''))).status,401);
 const first=await POST(req(action,0));assert.equal(first.status,200);
 const loaded:any=await (await GET(new Request(url,{headers:{'oai-authenticated-user-id':'planner'}}))).json();assert.equal(loaded.state.name,'Persistent project');assert.equal(loaded.revision,1);
 const concurrent=await Promise.all([POST(req(action,1)),POST(req(action,1))]);assert.deepEqual(concurrent.map(r=>r.status).sort(),[200,409]);
 assert.equal((await POST(req(action,2,'different-user'))).status,200);
 const invalid=new Request(url,{method:'POST',headers:{origin:'https://evil.example','oai-authenticated-user-id':'planner'},body:JSON.stringify({revision:2,action})});assert.equal((await POST(invalid)).status,403);
});
