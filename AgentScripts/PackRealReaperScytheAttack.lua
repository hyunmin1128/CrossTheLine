local source = app.open(app.params["sheet"])
assert(source, "Missing redrawn sprite sheet")
local w, h = math.ceil(source.width / 4), math.ceil(source.height / 4)
local pad=32
local output = Sprite(w+pad*2, h+pad*2, ColorMode.RGB)
output.layers[1].name = "RealReaper redraw"
local image = source.layers[1]:cel(1).image
local duration = {160,120,100,100,50,50,50,60,80,100,110,110,100,100,120,180}
for i=1,16 do
  if i>1 then output:newEmptyFrame(i) end
  local frame = Image(w,h,ColorMode.RGB)
  local col, row = (i-1)%4, math.floor((i-1)/4)
  frame:drawImage(image, Point(-math.floor(col*source.width/4), -math.floor(row*source.height/4)))
  local existing=output.layers[1]:cel(i)
  if existing then output:deleteCel(existing) end
  local baseline=math.floor(h*.8)
  for yy=0,h-1 do
    local count=0
    for xx=0,w-1 do
      local p=frame:getPixel(xx,yy)
      local r,g,b,a=app.pixelColor.rgbaR(p),app.pixelColor.rgbaG(p),app.pixelColor.rgbaB(p),app.pixelColor.rgbaA(p)
      if a>128 and r>=28 and r<=80 and g>=24 and g<=75 and b>=20 and b<=75 and math.abs(r-g)<18 then count=count+1 end
    end
    if count>=w*.05 then baseline=yy end
  end
  local dy=math.floor(h*.8)-baseline
  output:newCel(output.layers[1],i,frame,Point(pad,pad+dy))
  output.frames[i].duration=duration[i]/1000
end
for _,item in ipairs({{"Attack",1,16},{"Windup",1,4},{"Swing",5,8},{"FollowThrough",9,12},{"Recovery",13,16}}) do
  local tag=output:newTag(item[2],item[3]);tag.name=item[1]
end
output:saveAs("D:/Unity/CrossTheLine/Assets/Sprite/Objects/Chapter3/Stage4/RealReaperScytheAttack-v3.aseprite")
print("Saved16 redrawn frames with attack phase tags. Cell="..w.."x"..h)
