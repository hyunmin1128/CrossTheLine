local root="D:/Unity/CrossTheLine/Assets/Sprite/Effect/Fasting/"
local dest=root.."Editable/"
app.fs.makeAllDirectories(dest)
local function pack(name,prefix,count,ms)
  local out=Sprite(64,64,ColorMode.RGB);out.layers[1].name="Artwork"
  for i=1,count do
    local src=app.open(root..prefix..(count==1 and "" or "_"..(i-1))..".png")
    assert(src,"Missing source "..prefix)
    if i>1 then out:newEmptyFrame(i) end
    local existing=out.layers[1]:cel(i);if existing then out:deleteCel(existing) end
    local image=Image(64,64,ColorMode.RGB)
    local cel=src.layers[1]:cel(1);image:drawImage(cel.image,cel.position)
    out:newCel(out.layers[1],i,image,Point(0,0));out.frames[i].duration=ms/1000
    src:close()
  end
  local tag=out:newTag(1,count);tag.name="Loop"
  return out
end
local bubble=pack("ShieldBubble","Bubble",4,250)
local fill=bubble:newLayer();fill.name="Blue membrane fill";fill.stackIndex=1
for i=1,4 do
  local image=Image(64,64,ColorMode.RGB)
  for y=0,63 do for x=0,63 do
    local u=(x-31.5)/26.5;local v=(y-31.5)/20
    if u*u+v*v<=1 then image:drawPixel(x,y,app.pixelColor.rgba(56,179,255,i==4 and 10 or 32)) end
  end end
  bubble:newCel(fill,i,image,Point(0,0))
end
bubble:saveAs(dest.."ShieldBubble.aseprite")
pack("PrayerLight","Prayer",4,200):saveAs(dest.."PrayerLight.aseprite")
pack("ShieldAcquireSpark","Spark",1,200):saveAs(dest.."ShieldAcquireSpark.aseprite")
pack("ShieldBreak","ShieldBreak",6,200):saveAs(dest.."ShieldBreak.aseprite")
local shield=app.open("D:/Unity/CrossTheLine/Assets/Sprite/Objects/ShieldOfFaith.png")
shield:saveAs(dest.."OrbitShield.aseprite")
local prayer=Sprite(128,128,ColorMode.RGB);prayer.layers[1].name="Soft fill"
local rim=prayer:newLayer();rim.name="Light rim"
local sparks=prayer:newLayer();sparks.name="Moving highlights"
for i=1,4 do
  if i>1 then prayer:newEmptyFrame(i) end
  local base=prayer.layers[1]:cel(i);if base then prayer:deleteCel(base) end
  local image=Image(128,128,ColorMode.RGB);local border=Image(128,128,ColorMode.RGB);local shine=Image(128,128,ColorMode.RGB)
  for y=0,127 do for x=0,127 do
    local u=(x-63.5)/36.8;local v=(y-63.5)/48;local d=math.sqrt(u*u+v*v)
    if d<=1 then image:drawPixel(x,y,app.pixelColor.rgba(255,217,97,14)) end
    if d>=.974 and d<=1.012 then border:drawPixel(x,y,app.pixelColor.rgba(255,217,97,204))
    elseif d>1.012 and d<1.06 then border:drawPixel(x,y,app.pixelColor.rgba(255,217,97,25)) end
  end end
  for n=0,7 do
    local a=n*math.pi/4+(i-1)*.15;local x=math.floor(63.5+math.cos(a)*36.8);local y=math.floor(63.5+math.sin(a)*48)
    for d=-1,1 do shine:drawPixel(x+d,y,app.pixelColor.rgba(255,240,192,210));shine:drawPixel(x,y+d,app.pixelColor.rgba(255,240,192,210)) end
  end
  prayer:newCel(prayer.layers[1],i,image,Point(0,0));prayer:newCel(rim,i,border,Point(0,0));prayer:newCel(sparks,i,shine,Point(0,0));prayer.frames[i].duration=.2
end
local tag=prayer:newTag(1,4);tag.name="Loop"
prayer:saveAs(dest.."PrayerAura.aseprite")
print("Saved six editable effect sources. Original PNGs preserved.")
