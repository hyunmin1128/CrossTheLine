local source = app.open("D:/Unity/CrossTheLine/Assets/Sprite/Objects/RealReaper.aseprite")
assert(source and #source.frames == 20, "Expected the authored 20-frame source")
local output = Sprite(source)
local sequence = {2,5,6,3,4,7,8,9,10,11,12,13,18,19,18,13,7,2}
local milliseconds = {160,120,80,70,70,60,50,50,50,50,60,70,70,60,120,80,80,180}
local function leaves(layers, result)
  for _,layer in ipairs(layers) do
    if layer.isGroup then leaves(layer.layers, result) else table.insert(result, layer) end
  end
end
local srcLayers, dstLayers = {}, {}
leaves(source.layers, srcLayers)
leaves(output.layers, dstLayers)
assert(#srcLayers == #dstLayers)
while #output.frames > 1 do output:deleteFrame(output.frames[#output.frames]) end
for _,layer in ipairs(dstLayers) do
  local cel = layer:cel(1)
  if cel then output:deleteCel(cel) end
end
while #output.tags > 0 do output:deleteTag(output.tags[#output.tags]) end
for i,index in ipairs(sequence) do
  if i > 1 then output:newEmptyFrame(i) end
  output.frames[i].duration = milliseconds[i] / 1000
  for j,layer in ipairs(srcLayers) do
    local cel = layer:cel(index + 1)
    if cel then
      local copied = output:newCel(dstLayers[j], i, Image(cel.image), cel.position)
      copied.opacity = cel.opacity
    end
  end
end
local tag = output:newTag(1, #sequence)
tag.name = "Attack"
output:saveAs("D:/Unity/CrossTheLine/Assets/Sprite/Objects/RealReaperAttack.aseprite")
print("Saved 18 attack frames, 1480ms. Source pixels and layers preserved.")
