import {toolDefinitions} from '../../lib/mcp-tools';
import {identity,sameOrigin,readControl,mutateControl,errorBody,json} from '../../lib/control-service';
export async function POST(request:Request){
 let requestId:string|number|null=null;
 try{
  sameOrigin(request);const raw=await request.text();if(raw.length>800000)return json({error:'Request too large'},413);const body=JSON.parse(raw);requestId=body.id??null;
  if(body.jsonrpc!=='2.0')return json({jsonrpc:'2.0',id:requestId,error:{code:-32600,message:'Invalid JSON-RPC request'}});
  let result:unknown;
  if(body.method==='server/discover')result={serverInfo:{name:'crdd',version:'4.0.0'},supportedVersions:['2026-07-28'],capabilities:{tools:{}}};
  else if(body.method==='initialize')result={protocolVersion:body.params?.protocolVersion||'2025-03-26',serverInfo:{name:'crdd',version:'4.0.0'},capabilities:{tools:{}}};
  else if(body.method==='notifications/initialized')return new Response(null,{status:204});
  else if(body.method==='tools/list')result={tools:toolDefinitions};
  else if(body.method==='tools/call'){
   try{const actor=identity(request),args=body.params?.arguments||{};let data:unknown;
    if(['crdd_read','orca_read'].includes(body.params?.name))data=await readControl(args,actor);
    else if(['crdd_write','orca_write'].includes(body.params?.name))data=await mutateControl(args,actor);
    else return json({jsonrpc:'2.0',id:requestId,error:{code:-32602,message:'Unknown tool'}});
    result={content:[{type:'text',text:JSON.stringify(data)}],structuredContent:data,isError:false};
   }catch(e){const x=errorBody(e);result={content:[{type:'text',text:JSON.stringify(x)}],structuredContent:x,isError:true}}
  }else return json({jsonrpc:'2.0',id:requestId,error:{code:-32601,message:'Method not found'}});
  return json({jsonrpc:'2.0',id:requestId,result});
 }catch{return json({jsonrpc:'2.0',id:requestId,error:{code:-32700,message:'Invalid request'}})}
}
