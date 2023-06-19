---@class Game.Application
local M = class()
local SM = require("ServiceManager")
-- local MockLoginState = require("Game.State.MockLoginState")
-- local LandingState = require("Game.State.LandingState")
-- local ChangeClothState = require("Game.State.ChangeCloth.ChangeClothState")
-- local SEDService = require("Game.Network.ServerEndData.ServerEndDataService")
-- local ns = SM.GetService(SM.SERVICE_TYPE.NATIVE)
-- local UIService = SM.GetService(SM.SERVICE_TYPE.UI)
-- local GV = CS.XiaoIceland.GlobalVariable
-- local MsgBox = require("UI.Panels.MessageBox")
-- local RestartState = require("Game.State.RestartState")
-- local BuildingInteractive = require("Game.Building.BuildingInteractive")
-- local EventType = require("EventType")
-- local cjson = require("cjson")
-- local AssetService = CS.Extend.Asset.AssetService
-- local VersionService = CS.XiaoIceland.Service.VersionService

_APP_VERSION = "1.0.2"
_APP_LUA_VERSION = "1.0.2"
_APP_RES_VERSION = "1.0.2"
_APP_NATIVE_VERSION = "2.3.0"

-- local ToastManager = require("UI.Panels.ToastManager")
-- local CoroutineAssetLoader = require("base.asset.CoroutineAssetLoader")

function M:ctor()
	_APP = self
	-- _APP.native = GV.NATIVE
	-- _APP.prod = GV.PROD
	-- _APP.platform = GV.PLATFORM
	-- _APP.playerInitState = "FirstLandingState"

	-- if #_APP.platform == 0 then
	-- 	_APP.platform = "PC"
	-- end
	-- _APP_VERSION = VersionService.PLATFORMVERSION
	-- if VersionService.Instance ~= nil then
	-- 	_APP_LUA_VERSION = VersionService.LUAVERSION
	-- 	self.isFirstLanded = false
	-- else
	-- 	_APP_LUA_VERSION = "2.0.8"
	-- end
	-- _APP_RES_VERSION = VersionService.RESVERSION
	-- self.trackData = {}

	-- local loadingRef = AssetService.Get():LoadGameObject(SM.GetConfigService().PathForFile("Loading.prefab"))
	-- local loadingGO = loadingRef:Instantiate()
	-- self.loading = loadingGO:GetLuaBinding(require("UI.Panels.Loading"))
	-- local loadingRoot = loadingGO.transform
	-- self.loadingGO = loadingGO
	-- local transitionRoot = SM.GetUIService().GetLayerRoot("Transition")
	-- loadingRoot:SetParent(transitionRoot, false)
	-- loadingRef:Dispose()
	-- warn("lua ver", _APP_LUA_VERSION,"res ver", _APP_RES_VERSION)
	-- self.isShutDown = false
	-- self.isSkipWelcome = false
	-- self.global = SM.GetGlobalEventService().GetGlobalDispatcher()
end

function M:Init()
	-- CoroutineAssetLoader.Create(function(loader)
	-- 	local icons = {}
	-- 	icons.infoRef = loader:LoadAsset(SM.GetConfigService().PathForFile("info.png"), "LoadSpriteAsync")
	-- 	icons.info = icons.infoRef:GetSprite()
	-- 	icons.errorRef = loader:LoadAsset(SM.GetConfigService().PathForFile("error.png"), "LoadSpriteAsync")
	-- 	icons.error = icons.errorRef:GetSprite()
	-- 	ToastManager.SetIcons(icons)
	-- 	self.toastIcons = icons
	-- end)

	-- -- native register
	-- ns.AddEvent("Landing", M.Landing, self)
	-- ns.AddEvent("UnLanding", M.UnLanding, self)
	-- ns.AddEvent("onBackPressed", M.OnBackPressed, self)
	-- ns.AddEvent("UpdateInfo", M.UpdateInfo, self)
	-- ns.AddEvent("IslandVisitors", M.UpdateUser, self)
	-- ns.AddEvent("SetVolume", M.SetVolume, self)
	-- ns.AddEvent("SetMainPageRedDot", M.SetRedDot, self)

	-- local native = CS.XiaoIceland.NativeManager.NATIVE
	-- if (not native) then
		-- self:Switch(MockLoginState.new())
	-- end

	-- local service = SM.GetService(SM.SERVICE_TYPE.TICK)
	-- service.Register(M.Tick, self)
	-- BuildingInteractive.Init()

	-- print("this is a print")
	-- warn("this is a warn")
	-- error("this is a error")
end

function M:OnBackPressed()
	if not UIService.CloseTopView() then
		if self.platform == "android" and _ServerEndData.GetAccountData().guide then
			CS.UnityEngine.Application.Quit(0)
			return
		end
		ns.BackPressed()
	end
end

function M:Landing(info)
	if info then
		if not _APP.prod then
			warn("Native Landing", table.dump_r(info))
		else
			warn("Native Landing", info["sceneName"])
		end
		self.track = info.track
		if self.track then
			self.trackData = cjson.decode(info.track)
		end
		local island = _ServerEndData.GetIslandData()
		island.owner = self:Str2Boolean(info["isOwner"])
		local originId = island.id
		island.id = info["islandId"]
		if originId ~= island.id then
			self:Dispatch("LandingIslandChanged", island.id)
		end

		local account = SEDService.GetAccountData()
		account.islandId = info["islandId"]
		--换装
		island.isNew = false
		account.guide = false
		_APP.loadingStartTime = SM.GetTickService().realtimeSinceStartup
		if info["sceneName"] == "ChangeCloth" then
			_APP:Switch(ChangeClothState.new(), account)
			return
		elseif info["sceneName"] == "BeginnerGuide" then
			island.isNew = true
			account.guide = true
			_APP.playerInitState = "BeginnerGuideState"
			local welcomeBankPath = SM.GetConfigService().PathForFile("LoadWelcomeBank.prefab")
			local welcomeBankRef = AssetService.Get():LoadGameObject(welcomeBankPath)
			self.welcomeBankGO = welcomeBankRef:Instantiate()
			CS.UnityEngine.GameObject.DontDestroyOnLoad(self.welcomeBankGO)
			welcomeBankRef:Dispose()
			_APP:Switch(ChangeClothState.new(), account)
			return
		end
		SM.GetNativeService().Track("LoadingIslandTrigger", {scenario_type = island.owner and "登岛" or "出访"})
	end
	self:Switch(LandingState.new())
end

local CheckUpdateType = CS.XiaoIceland.Service.VersionService.HasAnyUpdate
function M:CheckUpdate()
	if CheckUpdateType ~= nil then
		if not self.isFirstLanded then
			--first landed
			self.isFirstLanded = true
		else
			local account = SEDService.GetAccountData()
			CheckUpdateType(account.userId,function(needUpdate)
				warn("checkUpdate", needUpdate)
				if needUpdate then
					local dontNeedRestart = _VersionCompare(_APP_NATIVE_VERSION, "2.4.1")
					local MsgBoxCallbacks = {
						function()
							if dontNeedRestart then
								VersionService.IsChecking = true
								self:Switch(RestartState.new())
							end
						end
					}
					MsgBox.ShowMsgBox(dontNeedRestart and "hotfix_warning_new" or "hotfix_warning", MsgBoxCallbacks)
					--MsgBox.ShowMsgBox("hotfix_warning", MsgBoxCallbacks, "MostTop")
				end
			end)
		end
	end
end

-- local UnLandingState = require("Game.State.UnLandingState")
-- function M:UnLanding()
-- 	warn("Native UnLanding")
-- 	self:Switch(UnLandingState.new())
-- end

function M:Str2Boolean(str)
	str = string.lower(str)
	if str == "true" then
		return true
	end
	return false
end

-- local Account = require("Game.Network.ServerEndData.Account")
-- function M:UpdateInfo(info)
-- 	if _APP.prod then
-- 		warn("Update Info")
-- 	else
-- 		warn("Update Info", table.dump_r(info))
-- 	end
-- 	Account.ParseFromServerData(info)
-- 	local appVersion = info["appVersion"]
-- 	if appVersion ~= nil then
-- 		_APP_NATIVE_VERSION = appVersion
-- 	end
-- 	local qualityLevel = info.qualityLevel
-- 	self.qualityLevel = qualityLevel
-- 	self:UpdateTargetFrameRate()

-- 	_ServerEndData.UpdateWebSocketHeader()
-- end

function M:GetNativeTrackData()
	return self.trackData
end

function M:UpdateTargetFrameRate()
	if self.qualityLevel == "1" then
		CS.UnityEngine.Application.targetFrameRate = 60
	else
		CS.UnityEngine.Application.targetFrameRate = 30
	end
end

function M:UpdateUser(info)
	local island = _ServerEndData.GetIslandData()
	island.userCount = info["VisitorsNum"]
	self:Dispatch("SET_USER_COUNT", island.userCount)
end

function M:Tick()
	if CS.UnityEngine.Input.GetKeyDown(CS.UnityEngine.KeyCode.B) then
		self:Switch(RestartState.new())
	end
	
	local ok, err = xpcall(SEDService.Tick, debug.traceback)
	if not ok then
		error(err)
	end

	if not self.currentState then
		return
	end
	if self.stateSwitching then
		if not self.currentState:GetIsReady() then
			return
		else
			self.stateSwitching = false
			-- self.loading:Hide()
			warn("State Ready", self.currentState:GetStateName())
			self:Dispatch(EventType.LOADING_UI_HIDE)
		end
	end

	self.currentState:Update()
end

---@return Game.State.StateBase
function M:GetCurrentState()
	return self.currentState
end

function M:_PerformSwitch(state, param)
	if self.currentState and state and self.currentState:GetStateName() == state:GetStateName() then
		warn("Same state repeat switch", state:GetStateName())
		return
	end

	self.stateSwitching = true
	SM.GetUIService().BeforeSceneUnload()
	if self.currentState then
		self.currentState:PrepareExit()
		self.currentState:Exit()
		self.currentState = nil
	end

	self.currentState = state
	if self.currentState then
		warn("Entering", self.currentState:GetStateName())
		self.currentState:PrepareEnter()
		self.currentState:Enter(table.unpack(param))
	end
end

function M:SetRedDot(info)
	if not self.redDotNativeInfo then
		self.redDotNativeInfo = {}
	end
	self.redDotNativeInfo[info["Name"]] = info
	self:Dispatch("UPDATE_UI_REDOT")
end

function M:Dispatch(typ,data)
	if self.global then
		self.global:DispatchEvent(typ,data)
	end
end

function M:SetVolume(data)
	CS.XiaoIceland.Audio.AudioUtilities.SetRtpc("Volume_Overall", tonumber(data.volume))
end

---@param state Game.State.StateBase
function M:Switch(state, ...)
	if self.currentState == state then
		return
	end

	local param = table.pack(...)
	self:Dispatch(EventType.ON_SCENE_UNLOAD)
	-- self.loading:Show()
	self:_PerformSwitch(state, param)
end

function M:Shutdown()
	if self.isShutDown then
		return
	end
	self:Switch(nil)
	self.isShutDown = true
	if self.toastIcons then
		if self.toastIcons.infoRef then
			self.toastIcons.infoRef:Dispose()
		end
		if self.toastIcons.errorRef then
			self.toastIcons.errorRef:Dispose()
		end
	end
	
	-- if self.welcomeBankGO ~= nil then
	-- 	AssetService.Recycle(self.welcomeBankGO)
	-- end
	-- AssetService.Recycle(self.loadingGO)

	-- if _VersionCompare(_APP_NATIVE_VERSION,"2.4.1") and VersionService.Instance then
	-- 	VersionService.Instance:Clear()
	-- end
	
	-- ns.RemoveEvent("UpdateInfo", M.UpdateInfo)
	-- ns.RemoveEvent("Landing", M.Landing)
	-- ns.RemoveEvent("onBackPressed", M.OnBackPressed)
	-- ns.RemoveEvent("IslandVisitors", M.UpdateUser)
	-- ns.RemoveEvent("SetVolume", M.SetVolume)
	-- ns.RemoveEvent("SetMainPageRedDot", M.SetRedDot)
	-- BuildingInteractive.Shutdown()
	-- local socket = SEDService.GetSocket()
	-- if socket ~= nil then
	-- 	socket:Close()
	-- end
end

return M
