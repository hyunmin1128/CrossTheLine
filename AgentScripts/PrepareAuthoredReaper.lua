local source=app.open("D:/Unity/CrossTheLine/Assets/Sprite/Objects/Chapter3/Stage4/RealReaper.aseprite")
assert(source and #source.frames==16,"Expected the user's 16 newly drawn frames")
local output=Sprite(source)
while #output.tags>0 do output:deleteTag(output.tags[#output.tags]) end
local timings={180,150,140,140,120,120,40,40,40,40,40,40,40,40,40,40}
for i=1,16 do output.frames[i].duration=timings[i]/1000 end
local all=output:newTag(1,16);all.name="Attack"
local prepare=output:newTag(1,6);prepare.name="Windup"
local swing=output:newTag(7,16);swing.name="Swing"
output:saveAs("D:/Unity/CrossTheLine/Assets/Sprite/Objects/Chapter3/Stage4/RealReaperAttack-Authored.aseprite")
print("Saved authored 16-frame animation without repainting or changing canvas alignment")
