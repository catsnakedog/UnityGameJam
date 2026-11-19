import type {Metadata} from 'next';
import './globals.css';
export const metadata:Metadata={title:'CRDD',description:'기획 · 작업 등록 · 수신 · 검수',icons:{icon:'/favicon.svg'}};
export default function Layout({children}:{children:React.ReactNode}){return <html lang="ko"><body>{children}</body></html>}
