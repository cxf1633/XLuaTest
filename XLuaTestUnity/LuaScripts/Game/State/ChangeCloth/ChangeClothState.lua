local StateBase = require("Game.State.StateBase")
-- local EquipmentService = require("Game.State.ChangeCloth.EquipmentService")
-- local EventType = require("EventType")
local SM = require("ServiceManager")
-- local ChangeCloth = require("UI.Panels.ChangeCloth")
-- local NewUserGuide = require("UI.Panels.NewUserGuide")
-- local AvatarExtend = require("Game.AvatarExtend")
-- local MessageBox = require("UI.Panels.MessageBox")
-- local UIService = SM.GetUIService()
-- local TickService = SM.GetTickService()
-- local ClothBrowser = require("UI.Panels.ItemBrowser.CustomClothBrowser")
-- local ClothBrowserData = require("UI.Panels.ItemBrowser.ClothBrowserData").ClothBrowserData
-- local ChangeSceneEnv = require("UI.Panels.ChangeCloth.ChangeSceneEnv")
-- local DIYCloth = require("UI.Panels.DIYCloth")

local ConfigService = SM.GetConfigService()

---@class Game.State.ChangeClothState : Game.State.StateBase
---@field clothBrowserData ClothBrowserData
local M = class(StateBase)
local avatar = {}
---@type UI.Panels.ChangeCloth.ChangeSceneEnv
local environment = nil
M.clothBrowserData = {}
M.defaultModelInfo = {}

function M:Enter()
	self.ready = false
	-- self.dressUpStartTime = TickService.totalTime
	-- SM.GetNativeService().Track("LoadingIslandTrigger", { scenario_type = "换装" })
	-- SM.GetNativeService().TrackLoadingIsland("LoadingIslandBegin", { scenario_type = "换装" })
	-- SM.GetNativeService().Landed()
	--env
	local configService = SM.GetConfigService()
	local sceneLoadManager = CS.Extend.SceneManagement.SceneLoadManager.Get()
	sceneLoadManager:LoadSceneAsync(configService.PathForFile("Change.unity"), false, function ()
		-- SM.RegisterService(EquipmentService.ServiceId, EquipmentService)
		-- self:InitEquipment()
	end)
	-- self.global = SM.GetService(SM.SERVICE_TYPE.GLOBAL_EVENT).GetGlobalDispatcher()
	-- self.global:AddEventListener(EventType.CHANGECLOTH_CUSTOM_FINISH, M.OnCustomClothFinish, self)
	-- self.global:AddEventListener(EventType.CHANGECLOTH_CUSTOM_START, M.OnCustomClothStart, self)
	-- if _ServerEndData.GetSocket() then
	-- 	_ServerEndData.GetSocket():SetPauseSocketEvent(true)
	-- end
end

function M:InitEquipment()
	---@type Game.State.ChangeCloth.EquipmentService
	self.equipmentService = SM.GetService(SM.SERVICE_TYPE.EQUIPMENT)
	local configService = SM.GetConfigService()
	local island = _ServerEndData.GetIslandData()
	self.equipmentService.StartLoad(
	---@param data SEquipmentData
		function (data)
			--http request fail
			if not data then
				local boxId = "network_reconnect"
				local try = function ()
					self:InitEquipment()
				end
				local landing = function ()
					SM.GetNativeService().TrackLoadingIsland("LoadingIslandEnd", { scenario_type = "换装" })
					_APP:Landing()
				end
				local msgCallbacks = {
					landing,
					try
				}
				if _ServerEndData.GetIslandData().isNew then
					msgCallbacks = { try }
					boxId = "network_connect_error"
				end
				MessageBox.ShowMsgBox(boxId, msgCallbacks, "MostTop")
				return
			end
			AvatarExtend(avatar)

			local faceConfigs = configService.GetConfig("FaceCreation")
			local faceConfig = faceConfigs[data.ModelInfo.FaceId]
			local path = configService.PathForFile("RemotePlayer.prefab")
			---@type ModelInfo
			M.defaultModelInfo = data.ModelInfo
			local modelInfo = data.ModelInfo:Clone()
			avatar:SetupEquipment(modelInfo.AppearanceJson)
			self.equipmentService.LoadPlayer(faceConfig, path, function (avatarGo, faceData)
				avatar:SetupAvatar(avatarGo, CS.UnityEngine.Vector3(0, 0.025, 0), 0, faceData)
				avatar:GetAvatarEquipment():EquipAll()
				local envType, uiId, bindType = "env", "ChangeCloth", ChangeCloth
				if island.isNew then
					envType, uiId, bindType = "env_new", "NewUserGuide", NewUserGuide
				end
				environment = CS.UnityEngine.GameObject.Find(envType):GetLuaBinding(ChangeSceneEnv)
				environment:Init()
				environment.SetPanTargetModel(avatar:GetGameObject())
				M.clothBrowserData = ClothBrowserData.new(
					modelInfo,
					data.CanDiy,
					data.EquipmentList,
					data.CustomEquipmentList,
					M.OnClothChanged
				)
				self.headerView = UIService.Show(uiId, function (err, go)
					local ui = go:GetLuaBinding(bindType)
					ui:Init(avatar)
					SM.GetNativeService().TrackLoadingIsland("LoadingIslandEnd", { scenario_type = "换装" })
					self.ready = true
					environment:SwitchCameraWithEquipPart("normal")
					UIService.AfterSceneLoaded()
				end)
			end)
		end)
end

---@param args BrowserItemSelectedEventArgs
function M.OnClothChanged(args)
	if table.empty(avatar) then
		return
	end
	if args.ItemData then
		local location = args.ItemData:GetType()
		-- body
		if location == "face" then
			local faceConfigs = ConfigService.GetConfig("FaceCreation")
			local faceData = faceConfigs[args.ItemData.Id].facedata
			M.clothBrowserData.modelInfor.FaceId = args.ItemData.Id
			EquipmentService.GetFaceDataJson(faceData, function (text)
				avatar:ReloadPlayerFace(text, false)
			end)
		else
			if args.IsSelected then
				avatar:GetAvatarEquipment():ChangeOne(location, args.ItemData:Convert2EquipmentData(), function ()
					M.PlayRandomAnim(location)
				end)
			else
				avatar:GetAvatarEquipment():Unequip(location)
			end
		end
		environment:SwitchCameraWithEquipPart(location)
	else
		environment:SwitchCameraWithEquipPart(args.Tab)
	end
	local isChangedFromDef = not M.defaultModelInfo:Equals(M.clothBrowserData.modelInfor)
	SM.GetGlobalEventService().GetGlobalDispatcher():DispatchEvent("ClothModify", args, isChangedFromDef)
end

---@param modelName string
---@param callback function<boolean|SPerpareData|nil>
function M:SaveClothInfo(modelName, callback)
	if self.pendingSReturn then
		return
	end
	self.pendingSReturn = true
	---@type UI.Panels.ItemBrowser.ClothBrowser
	local clothBrowser = self.clothBrowserPanel.view:GetLuaBinding(ClothBrowser)
	local allEquipData = {}
	local selectedItems = clothBrowser:GetAllSelctedItems()
	for key, value in pairs(selectedItems) do
		if key == "face" then
			M.clothBrowserData.modelInfor.FaceId = selectedItems.face.Id
		else
			if value:CanShare() then
				allEquipData[value.Location] = value:Convert2EquipmentData()
			end
		end
	end
	if not string.isNullOrEmpty(modelName) then
		M.clothBrowserData.modelInfor.Name = modelName
	end
	M.clothBrowserData.modelInfor.AppearanceJson = allEquipData
	local endTime = TickService.totalTime
	local dressUpDuration = endTime - self.dressUpStartTime
	self.equipmentService.SetInfo(M.clothBrowserData.modelInfor, function (resp)
		self.pendingSReturn = false
		callback(resp)
	end, dressUpDuration)
end

---@param location string
function M.PlayRandomAnim(location)
	local mapping = ConfigService.GetConfig("ChangeClothAnimationMapping")
	if mapping[location] then
		local anim = mapping[location].animationId
		if not string.isNullOrEmpty(anim) then
			local anims = string.split(anim, ',')
			local ri = math.random(1, table.count(anims))
			avatar:GetAnimationLoader():LoadConfig(anims[ri])
			avatar:GetAnimationLoader():LoadAndPlayAnimation(-1)
		end
	end
end

---@param posterData PosterData
---@return Texture2D | nil
function M.GeneratePoster(posterData)
	if environment == nil then
		return
	end
	return environment:GeneratePoster(posterData)
end

---@param textures Texture2D[] | nil
---@param topHeight integer | nil
function M.InputTexture2Model(textures, topHeight)
	if environment == nil then
		return
	end
	environment:InputTexture2Model(textures, topHeight)
end

---@param equipDataCtx EquipmentItemDataContext
function M:OnCustomClothStart(equipDataCtx)
	environment:SwitchCameraWithEquipPart("normal")
	self:ShowDIYRoom(equipDataCtx)
end

---@param equipDataCtx EquipmentItemDataContext
function M:OnCustomClothFinish(equipDataCtx)
	if equipDataCtx and equipDataCtx.bindListItem.IsDiyItem then
		---@type Game.Equipment.AvatarEquipment
		local avatarEquipment = avatar:GetAvatarEquipment()
		local equipData = equipDataCtx.bindListItem:Convert2EquipmentData()
		if avatarEquipment:HasEquip(equipData) then
			avatarEquipment:ChangeCustomMat(equipDataCtx.bindListItem.Location, equipData.Custom)
		end
	end
	environment:ResetRoom()
end

---@param gender integer
function M:ResetPlayerGender(gender)
	if avatar == nil or avatar == {} then
		return
	end
	local clothBrowserData = M.clothBrowserData
	if clothBrowserData.modelInfor.Gender == gender then
		return
	end
	clothBrowserData.modelInfor.Gender = gender
	clothBrowserData.modelInfor.AppearanceJson = EquipmentService.GetGenderDefaultEquipData(gender)
	local faceConfigs = ConfigService.GetConfig("FaceCreation")
	local faceDataId = faceConfigs[clothBrowserData.modelInfor.FaceId].facedata
	EquipmentService.GetFaceDataJson(faceDataId, function (faceData)
		avatar:ReloadPlayerAllEquip(clothBrowserData.modelInfor,faceData)
		local clothBrowser = self.clothBrowserPanel.view:GetLuaBinding(ClothBrowser)
		clothBrowser:UpdateBrowserData(clothBrowserData)
		clothBrowser:RefreshCurrentTab()
	end)
end

---@param refreshData boolean@ if true will refresh browserData
---@param callback function|nil
function M:ShowFittingRoom(refreshData, callback)
	if self.diyPanel then
		self.diyPanel:SetVisible(false)
	end
	self.headerView:SetVisible(true)
	environment.SetPanTargetModel(avatar:GetGameObject())
	if refreshData then
		EquipmentService.GetInfo(
		---@param data SEquipmentData
			function (data)
				local curModelInfo = M.clothBrowserData.modelInfor
				local browserData = ClothBrowserData.new(
					curModelInfo,
					data.CanDiy,
					data.EquipmentList,
					data.CustomEquipmentList,
					M.OnClothChanged
				)
				M.clothBrowserData = browserData
				self:ShowClothBrowser(M.clothBrowserData, callback)
			end
		)
		return
	end
	self:ShowClothBrowser(M.clothBrowserData, callback)
end

---@private
---@param clothBrowserData ClothBrowserData
---@param callback function|nil
function M:ShowClothBrowser(clothBrowserData, callback)
	if self.clothBrowserPanel then
		self.clothBrowserPanel:SetVisible(true)
		---@type UI.Panels.ItemBrowser.ClothBrowser
		local clothBrowser = self.clothBrowserPanel.view:GetLuaBinding(ClothBrowser)
		clothBrowser:UpdateBrowserData(clothBrowserData)
		if callback then
			callback(clothBrowser)
		end
	else
		self.clothBrowserPanel = UIService.Show("ClothBrowser", function (err, go)
			---@type UI.Panels.ItemBrowser.ClothBrowser
			local clothBrowser = go:GetLuaBinding(ClothBrowser)
			clothBrowser:Init(clothBrowserData)
			if callback then
				callback(clothBrowser)
			end
		end)
	end
end

---@param equipDataCtx EquipmentItemDataContext
function M:ShowDIYRoom(equipDataCtx)
	local config = ConfigService.GetConfigRow("PartSelection", equipDataCtx.bindListItem.ConfigId)
	local assetPath = config.previewPrefab
	local assetStandPath = config.previewStandPrefab
	if self.diyPanel then
		environment:LoadModel(assetPath, assetStandPath, function ()
			self.headerView:SetVisible(false)
			self.clothBrowserPanel:SetVisible(false)
			self.diyPanel:SetVisible(true)
			if self.diyPanel.view then
				---@type UI.Panels.DIYCloth
				local diyCloth = self.diyPanel.view:GetLuaBinding(DIYCloth)
				diyCloth:Init(equipDataCtx, M.GeneratePoster, M.InputTexture2Model)
			end
			environment:SwitchCustomRoom()
		end)
	else
		self.diyPanel = UIService.Show("CustomCloth", function (err, go)
			environment:LoadModel(assetPath, assetStandPath, function ()
				---@type UI.Panels.DIYCloth
				local diyCloth = go:GetLuaBinding(DIYCloth)
				--init
				diyCloth:Init(equipDataCtx, M.GeneratePoster, M.InputTexture2Model)
				self.headerView:SetVisible(false)
				self.clothBrowserPanel:SetVisible(false)
				environment:SwitchCustomRoom()
			end)
		end)
	end
end

function M:ResetRoom()
	if self.diyPanel then
		self.diyPanel:SetVisible(false)
	end
	if self.clothBrowserPanel then
		self.clothBrowserPanel:SetVisible(false)
	end
	environment:ResetRoom()
end

function M:GetStateName()
	return "Change"
end

-- abstract
function M:Exit()
	if environment then
		environment:Release()
		environment = nil
	end
	self.global:RemoveEventListener(EventType.CHANGECLOTH_CUSTOM_FINISH, M.OnCustomClothFinish)
	self.global:RemoveEventListener(EventType.CHANGECLOTH_CUSTOM_START, M.OnCustomClothStart)

	SM.UnregisterService(EquipmentService.ServiceId)
	if self.headerView then
		UIService.Hide(self.headerView)
	end

	if self.clothBrowserPanel then
		UIService.Hide(self.clothBrowserPanel)
	end

	if self.diyPanel then
		UIService.Hide(self.diyPanel)
	end

	if not table.empty(avatar) then
		avatar:Destroy()
		avatar = {}
	end
	local sceneLoadManager = CS.Extend.SceneManagement.SceneLoadManager.Get()
	sceneLoadManager:UnloadScene(SM.GetConfigService().PathForFile("Change.unity"))

	if _ServerEndData.GetSocket() then
		_ServerEndData.GetSocket():SetPauseSocketEvent(false)
	end
end

function M:GetIsReady()
	return self.ready
end

return M
