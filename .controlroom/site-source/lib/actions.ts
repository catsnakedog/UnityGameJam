import {z} from 'zod';
import {State,Task,Report,taskSchema,reportSchema,rid,risks,emptyState,splitPlan} from './model';
const txt=z.string().max(12000);const req=z.string().min(1).max(12000);
const actions=z.discriminatedUnion('type',[
 z.object({type:z.literal('task'),task:taskSchema,reason:txt}),
 z.object({type:z.literal('claim'),id:txt,owner:z.string().min(1).max(80)}),
 z.object({type:z.literal('release'),id:txt}),z.object({type:z.literal('ack'),id:txt}),
 z.object({type:z.literal('report'),report:reportSchema}),
 z.object({type:z.literal('review'),id:txt,decision:z.enum(['approve','reject']),feedback:req}),
 z.object({type:z.literal('integrate'),riskIds:z.array(txt).min(1),owner:req}),
 z.object({type:z.literal('contract'),contract:z.object({id:req,name:req,schema:req,version:z.number().int().positive(),confirmed:z.boolean(),owner:req})}),
 z.object({type:z.literal('shared'),file:z.object({path:req,owner:req,rule:req})}),
 z.object({type:z.literal('plan'),text:req}),
 z.object({type:z.literal('import'),tasks:z.array(taskSchema).min(1).max(100),requirements:z.array(z.object({id:req,text:req})).max(100)}),
 z.object({type:z.literal('settings'),name:req,members:z.array(z.string().min(1).max(80)).max(100)}),
 z.object({type:z.literal('start'),name:req}),z.object({type:z.literal('archive'),id:txt})
]);
export function applyAction(state:State,input:unknown,actor:{id:string;name:string}){
 const s=structuredClone(state),a=actions.parse(input);let title='',detail='',taskId:string|undefined;
 const find=(id:string)=>{const t=s.tasks.find(t=>t.id===id);if(!t)throw new Error('작업을 찾을 수 없습니다.');taskId=id;return t;};
 const planner=()=>{if(!actor.id)throw new Error('로그인이 필요합니다.');};
 const nextId=()=>`TASK-${String(Math.max(0,...s.tasks.map(t=>Number(t.id.replace('TASK-',''))||0))+1).padStart(3,'0')}`;
 if(a.type==='start'){planner();return emptyState(a.name,actor.id);}
 if(a.type==='task'){const old=s.tasks.find(t=>t.id===a.task.id);if(old){if(!a.reason.trim())throw new Error('명세 변경 사유를 입력하세요.');if(old.status==='완료')throw new Error('완료 작업의 변경은 새 작업으로 등록하세요.');const updated={...a.task,status:old.status==='검수 대기'?'재작업':old.status,version:old.version+1,ackVersion:old.ackVersion,approval:'승인 대기' as const,owner:old.owner};s.tasks=s.tasks.map(t=>t.id===old.id?updated:t);title='작업 명세 변경';detail=`v${old.version} → v${updated.version} · ${a.reason}\n이전 명세: ${JSON.stringify(old)}\n새 명세: ${JSON.stringify(updated)}`;taskId=old.id;}else{planner();const t={...a.task,id:nextId(),version:1,ackVersion:0,status:'대기' as const,approval:'확인 필요' as const};s.tasks.push(t);title='새 작업 등록';detail=t.title;taskId=t.id;}}
 if(a.type==='claim'){const t=find(a.id);if(t.owner&&t.owner!==a.owner)throw new Error(`${t.owner} 담당 작업입니다. 소유권을 먼저 해제하세요.`);if(!['대기','재작업','진행 중'].includes(t.status))throw new Error('현재 상태에서는 작업을 시작할 수 없습니다.');if(!s.members.includes(a.owner))throw new Error('팀에 등록된 담당자를 선택하세요.');t.owner=a.owner;t.status='진행 중';t.ackVersion=t.version;title='작업 선점';detail=a.owner;}
 if(a.type==='release'){const t=find(a.id);if(t.status==='완료')throw new Error('완료된 작업입니다.');t.owner='';t.status='대기';title='선점 해제';detail='로컬 파일은 변경하지 않습니다.';}
 if(a.type==='ack'){const t=find(a.id);t.ackVersion=t.version;title='최신 명세 확인';detail=`v${t.version} 확인`}
 if(a.type==='report'){const t=find(a.report.taskId);if(!t.owner)throw new Error('담당자를 선점한 뒤 보고서를 제출하세요.');if(!['진행 중','재작업'].includes(t.status))throw new Error('진행 중 또는 재작업 상태에서 제출하세요.');if(t.version!==a.report.version||t.ackVersion!==t.version)throw new Error('최신 명세를 확인하고 보고서를 작성하세요.');if(t.requirements.some(id=>!a.report.coverage.some(c=>c.id===id)))throw new Error('모든 요구사항의 구현 여부를 기록하세요.');if(t.criteria.some(text=>!a.report.criteria.some(c=>c.text===text)))throw new Error('모든 완료 조건의 충족 여부를 기록하세요.');s.reports.push({...a.report,id:rid(),at:new Date().toISOString()});t.status='검수 대기';t.approval='승인 대기';title='완료 보고서 제출';detail='기획자 검수 대기';}
 if(a.type==='review'){planner();const t=find(a.id);if(t.status!=='검수 대기')throw new Error('검수 대기 작업만 검수할 수 있습니다.');const r=s.reports.filter(r=>r.taskId===t.id).at(-1);if(a.decision==='approve'){if(!r)throw new Error('제출된 보고서가 필요합니다.');t.status='완료';t.approval='승인';title='기획자 승인';}else{t.status='재작업';t.approval='반려';title='반려 · 재작업 요청';}detail=a.feedback;}
 if(a.type==='integrate'){planner();const selected=risks(s).filter(r=>a.riskIds.includes(r.id));if(!selected.length)throw new Error('이미 해결되었거나 변경된 충돌입니다. 새로고침하세요.');const ids=[...new Set(selected.flatMap(r=>r.taskIds))];if(s.tasks.some(t=>t.kind==='통합'&&t.status!=='완료'&&t.related.length===ids.length&&ids.every(id=>t.related.includes(id))))throw new Error('이 작업들을 위한 통합 작업이 이미 있습니다.');const t:Task={id:nextId(),title:`${selected[0].label} 조정`,description:selected.map(r=>r.title+'\n'+r.detail).join('\n\n'),group:'통합 및 조정',phase:'',owner:a.owner,status:'대기',approval:'확인 필요',scope:[],interfaces:[],contracts:[],depends:[],requirements:[],criteria:['관련 작업의 수정 영역과 소유권을 분리한다.','인터페이스·데이터 계약을 합의하고 관련 명세에 반영한다.','관련 충돌이 해소되었는지 확인한다.'],version:1,ackVersion:0,kind:'통합',related:ids,archive:false};s.tasks.push(t);title='통합 작업 생성';detail=`${t.id} · ${a.owner} · ${ids.join(', ')}`;taskId=t.id;}
 if(a.type==='contract'){planner();const old=s.contracts.find(c=>c.id===a.contract.id);const c={...old,...a.contract,version:old?old.version+1:1,updatedAt:new Date().toISOString()};s.contracts=s.contracts.filter(x=>x.id!==c.id);s.contracts.push(c);for(const t of s.tasks.filter(t=>t.contracts.includes(c.id)&&!t.archive&&t.status!=='완료')){t.version++;t.approval='승인 대기';if(t.status==='검수 대기')t.status='재작업';}title='공용 데이터 계약 변경';detail=`${c.id} v${c.version} · ${c.confirmed?'확정':'확인 필요'}`;}
 if(a.type==='shared'){planner();s.shared=s.shared.filter(f=>f.path!==a.file.path);s.shared.push(a.file);title='공용 파일 규칙 변경';detail=a.file.path;}
 if(a.type==='settings'){planner();s.name=a.name;s.members=[...new Set(a.members)];title='프로젝트 설정 변경';detail=a.name;}
 if(a.type==='plan'){planner();s.plan=a.text;title='기획서 저장';detail='로컬 관제탑SKill에서 실제 프로젝트 분석 후 계획 제출';}
 if(a.type==='import'){planner();const ids=a.tasks.map(t=>t.id);if(new Set(ids).size!==ids.length||ids.some(id=>s.tasks.some(t=>t.id===id)))throw new Error('작업 ID가 중복됩니다. 새 ID를 사용하세요.');const reqs=[...s.requirements,...a.requirements];if(new Set(reqs.map(r=>r.id)).size!==reqs.length)throw new Error('요구사항 ID가 중복됩니다.');for(const t of a.tasks){if(t.depends.some(id=>!ids.includes(id)&&!s.tasks.some(x=>x.id===id)))throw new Error('선행 작업 ID를 찾을 수 없습니다.');if(t.requirements.some(id=>!reqs.some(r=>r.id===id)))throw new Error('요구사항 ID를 찾을 수 없습니다.');}s.requirements=reqs;s.tasks.push(...a.tasks.map(t=>({...t,status:'대기' as const,approval:'확인 필요' as const,version:1,ackVersion:0,archive:false})));title='CRDD 작업 계획 가져오기';detail=`${a.tasks.length}개 작업 · 기획자 등록`;}
 if(a.type==='archive'){planner();const t=find(a.id);t.archive=!t.archive;title=t.archive?'작업 보관':'작업 복원';detail=t.title;}
 s.history.unshift({id:rid(),taskId,title,detail,at:new Date().toISOString(),actor:actor.name});return s;
}
