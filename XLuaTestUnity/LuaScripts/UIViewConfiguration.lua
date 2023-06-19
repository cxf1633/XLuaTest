local Configuration = CS.Extend.UI.UIViewConfiguration.Configuration
local AssetReference = CS.Extend.Asset.AssetReference
local UILayer = CS.Extend.UI.UILayer
local CloseOption = CS.Extend.UI.CloseOption
local c
local configurations = {}
c = Configuration()
c.Name = "MockLogin"
c.UIView = AssetReference("ca988694c1ddd7445b4e113620394645")
c.BackgroundFx = AssetReference("")
c.FullScreen = false
c.Transition = AssetReference("")
c.AttachLayer = UILayer.Dialog
c.CloseMethod = CloseOption.None
c.CloseButtonPath = ""
configurations.MockLogin = c
return configurations
