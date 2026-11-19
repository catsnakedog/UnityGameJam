import {env} from 'cloudflare:workers';
import {emptyState,State} from './model';
import {Actor,ControlError,normalize,control,readOperation,writeOperation,webControl,ensure} from './control';
import initialPlan from './unitygamejam-plan.json';

export function identity(request:Request):Actor{const id=request.headers.get('oai-authenticated-user-id');ensure(id,'unauthenticated','ChatGPT로 로그인하거나 관제탑 플러그인을 연결해 주세요.',401);let name=request.headers.get('oai-authenticated-user-full-name')||request.headers.get('oai-authenticated-user-email')||'팀원';try{if(request.headers.get('oai-authenticated-user-full-name-encoding')==='percent-encoded-utf-8')name=decodeURIComponent(name)}catch{}return {id,name}}
export function sameOrigin(request:Request){const origin=request.headers.get('origin');ensure(!origin||origin===new URL(request.url).origin,'origin','허용되지 않은 요청입니다.',403)}
export async function loadState(actor:Actor){
 const query=env.DB!.prepare('SELECT revision,data FROM workspaces WHERE id=?').bind('main');
 let row=await query.first<{revision:number;data:string}>();
 if(!row){
  const state=webControl(emptyState('UnityGameJam',actor.id),{type:'plan_propose',registration:'automatic',plan:initialPlan},actor);
  await saveState(state,0,false);
  row=await query.first<{revision:number;data:string}>();
 }
 ensure(row,'workspace_missing','작업 목록을 불러올 수 없습니다.',503);
 return {state:normalize(JSON.parse(row.data)),revision:row.revision,exists:true};
}
export async function saveState(state:State,revision:number,exists:boolean){const result=exists?await env.DB!.prepare('UPDATE workspaces SET data=?,revision=revision+1 WHERE id=? AND revision=?').bind(JSON.stringify(state),'main',revision).run():await env.DB!.prepare('INSERT OR IGNORE INTO workspaces(id,revision,data) VALUES(?,?,?)').bind('main',1,JSON.stringify(state)).run();return !!result.meta.changes}
function canonical(value:unknown):string{if(Array.isArray(value))return '['+value.map(canonical).join(',')+']';if(value&&typeof value==='object')return '{'+Object.entries(value).sort(([a],[b])=>a.localeCompare(b)).map(([k,v])=>JSON.stringify(k)+':'+canonical(v)).join(',')+'}';return JSON.stringify(value)}
async function fingerprint(value:unknown){const bytes=await crypto.subtle.digest('SHA-256',new TextEncoder().encode(canonical(value)));return Array.from(new Uint8Array(bytes),x=>x.toString(16).padStart(2,'0')).join('')}
export async function readControl(input:unknown,actor:Actor){const {state,revision}=await loadState(actor);return {ok:true,revision,...readOperation(state,input,actor)}}
export async function mutateControl(input:unknown,actor:Actor){
 const raw=input as {request_id?:unknown};ensure(typeof raw?.request_id==='string'&&raw.request_id.length>0,'request_id','request_id가 필요합니다.');const hash=await fingerprint(input);
 for(let attempt=0;attempt<4;attempt++){
  const {state,revision,exists}=await loadState(actor);const prior=control(state).receipts.find(r=>r.userId===actor.id&&r.requestId===raw.request_id);
  if(prior){ensure(prior.fingerprint===hash,'idempotency_conflict','같은 request_id에 다른 본문을 사용할 수 없습니다.',409);return {...prior.result,replayed:true}}
  const changed=writeOperation(state,input,actor),result={...changed.result,revision:revision+1,request_id:raw.request_id};
  control(changed.state).receipts.push({userId:actor.id,requestId:raw.request_id,fingerprint:hash,result});
  if(await saveState(changed.state,revision,exists))return result;
 }
 throw new ControlError('concurrent_change','다른 작업과 겹쳤습니다. 같은 본문과 request_id로 재시도하세요.',409);
}
export function errorBody(e:unknown){return e instanceof ControlError?{error:e.message,code:e.code,details:e.details,status:e.status}:{error:e instanceof Error&&e.name==='ZodError'?'필수 항목과 입력 형식을 확인하세요.':'요청을 처리하지 못했습니다.',code:'invalid_or_failed',status:400}}
export const json=(data:unknown,status=200)=>Response.json(data,{status,headers:{'Cache-Control':'no-store'}});
