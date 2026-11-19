// Execute route SQL against SQLite with D1's prepared-statement API.
import {DatabaseSync} from 'node:sqlite';
const db=new DatabaseSync(':memory:');
db.exec('CREATE TABLE workspaces(id TEXT PRIMARY KEY,revision INTEGER NOT NULL DEFAULT 0,data TEXT NOT NULL)');
export const env={
 DB:{
  prepare(sql:string){
   return {
    bind(...args:any[]){
     return {
      async first(){return db.prepare(sql).get(...args)||null},
      async run(){const result=db.prepare(sql).run(...args);return {meta:{changes:Number(result.changes)}}}
     };
    }
   };
  }
 }
};
