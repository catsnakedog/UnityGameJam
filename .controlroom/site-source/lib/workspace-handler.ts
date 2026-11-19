import {applyAction} from './actions';
import {identity,sameOrigin,loadState,saveState,errorBody,json} from './control-service';
import {webControl,control,normalize,ensure} from './control';
export async function GET(request:Request){try{const actor=identity(request);const {state,revision}=await loadState(actor);return json({state,revision,actor})}catch(e){const x=errorBody(e);return json(x,x.status)}}
export async function POST(request:Request){try{
 sameOrigin(request);const actor=identity(request),raw=await request.text();if(raw.length>800000)return json({error:'요청 데이터가 너무 큽니다.'},413);
 const body=JSON.parse(raw),loaded=await loadState(actor);ensure(body.revision===loaded.revision,'revision_conflict','다른 창이나 스킬에서 변경되었습니다. 새로고침 후 다시 시도하세요.',409);
 const a=body.action,previous=loaded.state;
 if(a.type==='claim'){const claim=control(previous).claims.find(c=>c.taskId===a.id);ensure(!claim,'claimed','다른 연결에서 이미 선점한 작업입니다. 먼저 명시적으로 해제하세요.',409)}
 const next=['connection_review','plan_review','control_settings','plan_propose'].includes(a.type)?webControl(previous,a,actor):normalize(applyAction(previous,a,actor));
 if(a.type==='claim'){const t=next.tasks.find(t=>t.id===a.id)!;control(next).claims.push({taskId:t.id,userId:actor.id,connectionId:'web:'+actor.id,startedAt:new Date().toISOString(),snapshot:structuredClone(t)})}
 if(a.type==='release'||a.type==='report')control(next).claims=control(next).claims.filter(c=>c.taskId!==(a.id||a.report.taskId));
 ensure(await saveState(next,loaded.revision,loaded.exists),'concurrent_change','동시 변경이 발생했습니다. 새로고침 후 다시 시도하세요.',409);
 return json({state:next,revision:loaded.revision+1,actor});
 }catch(e){const x=errorBody(e);return json(x,x.status)}}
