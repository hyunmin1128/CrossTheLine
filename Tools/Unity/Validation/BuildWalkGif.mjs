import fs from 'node:fs/promises';
import zlib from 'node:zlib';

function decode(png) {
  let pos=8,w,h,parts=[];
  while(pos<png.length){const len=png.readUInt32BE(pos),type=png.toString('ascii',pos+4,pos+8),data=png.subarray(pos+8,pos+8+len);if(type==='IHDR'){w=data.readUInt32BE(0);h=data.readUInt32BE(4);if(data[8]!==8||data[9]!==6)throw Error('Expected RGBA8');}if(type==='IDAT')parts.push(data);pos+=12+len;}
  const raw=zlib.inflateSync(Buffer.concat(parts)),out=Buffer.alloc(w*h*4),stride=w*4;
  const paeth=(a,b,c)=>{const p=a+b-c,pa=Math.abs(p-a),pb=Math.abs(p-b),pc=Math.abs(p-c);return pa<=pb&&pa<=pc?a:pb<=pc?b:c;};
  for(let y=0;y<h;y++){const filter=raw[y*(stride+1)];for(let x=0;x<stride;x++){const v=raw[y*(stride+1)+1+x],a=x>=4?out[y*stride+x-4]:0,b=y?out[(y-1)*stride+x]:0,c=y&&x>=4?out[(y-1)*stride+x-4]:0;out[y*stride+x]=(v+(filter===1?a:filter===2?b:filter===3?Math.floor((a+b)/2):filter===4?paeth(a,b,c):0))&255;}}
  return {w,h,rgba:out};
}

export async function build(root){
  const frames=[];for(let i=0;i<4;i++)frames.push(decode(await fs.readFile(root+'/Assets/Sprite/Minigames/FastingTopDown/JesusForwardWalk/Walk_'+i+'.png')));
  const palette=[[0,0,0]],map=new Map();for(const f of frames)for(let p=0;p<f.rgba.length;p+=4)if(f.rgba[p+3]>=128){const rgb=[...f.rgba.subarray(p,p+3)],key=rgb.join(',');if(!map.has(key)){map.set(key,palette.length);palette.push(rgb);}}
  if(palette.length>256)throw Error('Too many palette entries');
  const out=[],push=(...b)=>out.push(...b),word=n=>push(n&255,n>>8);
  push(...Buffer.from('GIF89a'));word(256);word(256);push(0xf7,0,0);for(let i=0;i<256;i++)push(...(palette[i]||[0,0,0]));
  push(0x21,0xff,11,...Buffer.from('NETSCAPE2.0'),3,1,0,0,0);
  for(let frame=0;frame<4;frame++){
    push(0x21,0xf9,4,9);word(frame%2?13:12);push(0,0,0x2c);word(0);word(0);word(256);word(256);push(0,8);
    const stream=[];let bits=0,acc=0,count=0;const code=c=>{acc|=c<<bits;bits+=9;while(bits>=8){stream.push(acc&255);acc>>>=8;bits-=8;}};
    code(256);
    for(let y=0;y<256;y++)for(let x=0;x<256;x++){if(count===200){code(256);count=0;}const i=(Math.floor(y/4)*64+Math.floor(x/4))*4,f=frames[frame].rgba;code(f[i+3]<128?0:map.get([...f.subarray(i,i+3)].join(',')));count++;}
    code(257);if(bits)stream.push(acc&255);for(let p=0;p<stream.length;p+=255){const b=stream.slice(p,p+255);push(b.length,...b);}push(0);
  }
  push(0x3b);const path=root+'/Docs/QA/ForwardWalk-20261116/forward-walk-v2.gif';await fs.writeFile(path,Buffer.from(out));return {path,frames:4,loopSeconds:.5,paletteColors:palette.length};
}
