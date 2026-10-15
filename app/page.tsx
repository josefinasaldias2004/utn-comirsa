import Market from './market';import {getChatGPTUser} from './chatgpt-auth';
export const dynamic='force-dynamic';
export default async function Home(){return <Market user={await getChatGPTUser()}/>}