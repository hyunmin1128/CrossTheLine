local source=app.open(app.params["source"])
assert(source,"Generated lava pit image missing")
app.activeSprite=source
app.command.SpriteSize{ui=false,width=64,height=64,method="nearest-neighbor"}
app.command.ColorQuantization{ui=false,maxColors=16,withAlpha=true}
app.command.ChangePixelFormat{ui=false,format="indexed",dithering="none"}
source.layers[1].name="Lava pit example"
source:saveAs("D:/Unity/CrossTheLine/Assets/Sprite/Objects/Chapter3/Stage4/Examples/Stage04_LavaPit_Example.aseprite")
source:saveCopyAs("D:/Unity/CrossTheLine/Assets/Sprite/Objects/Chapter3/Stage4/Examples/Stage04_LavaPit_Example.png")
print("Saved "..source.width.."x"..source.height..", palette entries="..#source.palettes[1]..", frames="..#source.frames)
app.command.SpriteSize{ui=false,width=384,height=384,method="nearest-neighbor"}
source:saveCopyAs("D:/Unity/CrossTheLine/Docs/QA/Stage04_LavaPit_Example-Preview.png")
