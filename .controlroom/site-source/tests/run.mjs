import * as esbuild from 'esbuild';
import {mkdtemp,rm} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import path from 'node:path';
import {spawnSync} from 'node:child_process';
const directory=await mkdtemp(path.join(tmpdir(),'orca-check-'));
try{
 await esbuild.build({entryPoints:['tests/domain.test.ts'],bundle:true,platform:'node',format:'cjs',outfile:path.join(directory,'domain.cjs')});
 await esbuild.build({entryPoints:['tests/route.test.ts'],bundle:true,platform:'node',format:'cjs',outfile:path.join(directory,'route.cjs'),plugins:[{name:'sqlite-d1',setup(build){build.onResolve({filter:/^cloudflare:workers$/},()=>({path:path.resolve('tests/mock-d1.ts')}))}}]});
 await esbuild.build({entryPoints:['tests/control.test.ts'],bundle:true,platform:'node',format:'cjs',outfile:path.join(directory,'control.cjs'),plugins:[{name:'sqlite-d1',setup(build){build.onResolve({filter:/^cloudflare:workers$/},()=>({path:path.resolve('tests/mock-d1.ts')}))}}]});
 const result=spawnSync(process.execPath,['--test',path.join(directory,'domain.cjs'),path.join(directory,'route.cjs'),path.join(directory,'control.cjs')],{stdio:'inherit'});
 process.exitCode=result.status??1;
}finally{await rm(directory,{recursive:true,force:true})}
