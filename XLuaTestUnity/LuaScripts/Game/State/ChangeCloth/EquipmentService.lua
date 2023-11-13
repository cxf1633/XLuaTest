local SM = require("ServiceManager")
local cjson = require "cjson"
local SEDService = require("Game.Network.ServerEndData.ServerEndDataService")
local GetInfoResponseData = require("Game.Network.ServerEndData.SEquipmentData").GetInfoResponseData
local InitInfoResponseData = require("Game.Network.ServerEndData.SEquipmentData").InitInfoResponseData
local BatchGetDIYEquipmentResponseData = require("Game.Network.ServerEndData.SEquipmentData").BatchGetDIYEquipmentResponseData
local DIYEquipmentResponseData = require("Game.Network.ServerEndData.SEquipmentData").DIYEquipmentResponseData
local RedemptionEquipResponseData = require("Game.Network.ServerEndData.SEquipmentData").RedemptionEquipResponseData
local WebRequest = require("Game.Network.WebRequest")
local EquipmentData = require("Game.Network.ServerEndData.SEquipmentData").EquipmentData


---@class Game.State.ChangeCloth.EquipmentService
---@field StartLoad function<function<SEquipmentData>>
---@field SetInfo function<ModelInfo,function<boolean|SPerpareData|nil>>
---@field SendDIYClothInfo function<any,function<CustomEquipmentData>>
---@field UpdateDIYClothInfo function<any,function<CustomEquipmentData>>
---@field SubmitDIYClothInfo function<string,function<boolean>>
---@field GetCustomEquipInfo function<string|string[]|table<string>,function<CustomEquipmentData[]>>
---@field GetCustomEquipInfoByCode function<string,function<CustomEquipmentData>>
---@field DeleteInfo function<string,boolean,string,integer,function<boolean>>@Id, isOn, location, auditStatus, callback
local M = { ServiceId = _ServiceManager.SERVICE_TYPE.EQUIPMENT }
M.EquipmentPartFlags = {
	NONE = 0x00000000,
	hair = 0x00000001,  --头发
	face = 0x00000002,  --脸型
	coat = 0x00000004,  --上衣
	pants = 0x00000008, --裤子
	headWear = 0x00000010, --头饰
	glasses = 0x00000020, --眼睛
	shoes = 0x00000040, --鞋子
}
M.EquipmentPartFlags.All = M.EquipmentPartFlags.hair | M.EquipmentPartFlags.face |
	M.EquipmentPartFlags.coat | M.EquipmentPartFlags.pants | M.EquipmentPartFlags.headWear |
	M.EquipmentPartFlags.glasses | M.EquipmentPartFlags.shoes


local Player
local loadContext
local active = true

function M.Init()
end

---@param callback  function<SEquipmentData>
function M.StartLoad(callback)
	M.faceDataJsons = {}
	active = true
	M.GetInfo(function (data)
		if not active then
			return
		end
		if not data then
			if callback then
				callback(data)
			end
			return
		end
		if callback then
			callback(data)
		end
	end)
end

---@param gender integer?
---@return table<string,EquipmentData>
function M.GetGenderDefaultEquipData(gender)
	M.genderBundleConfig = {}
	local bundleConfig = {}
	if gender == 1 then
		bundleConfig["hair"] = EquipmentData.new("1001009", 1, 1001009)
		bundleConfig["shoes"] = EquipmentData.new("4001005", nil, 4001005)
		bundleConfig["headWear"] = EquipmentData.new("6003005", nil, 6003005)
		bundleConfig["suit"] = EquipmentData.new("11004003", nil, 11004003)
	elseif gender == 2 then
		bundleConfig["hair"] = EquipmentData.new("1002009", 1, 1002009)
		bundleConfig["suit"] = EquipmentData.new("11002002", nil, 11002002)
		bundleConfig["shoes"] = EquipmentData.new("4002007", nil, 4002007)
	end
	return bundleConfig
end

function M.GetFaceDataJson(faceData, callback)
	if M.faceDataJsons[faceData] == nil then
		M.ReadFile(string.format("Avatar/Face/%s.json", faceData), function (text)
			M.faceDataJsons[faceData] = text
			callback(text)
		end)
	else
		callback(M.faceDataJsons[faceData])
	end
end

---@param callback  function<SEquipmentData>
function M.GetInfo(callback)
	local island = _ServerEndData.GetIslandData()
	local account = SEDService.GetAccountData()
	local reqData = {
		BeingId = account.userId,
		NewIsland = 0
	}
	if island.isNew then
		reqData.NewIsland = 1
	end
	WebRequest.Get("GetPlayerInfo", reqData, nil, function (response)
		local responseData = GetInfoResponseData.Decode(response)
		if responseData then
			if not responseData.Success then
				warn("EquipmentService GetInfo failed!", response)
				_ToastManager.ShowTip("Info", responseData.Message, 3)
				callback(nil)
			else
				if island.isNew then
					local gender = math.tointeger(responseData.Data.ModelInfo.Gender)
					responseData.Data.ModelInfo.AppearanceJson = M.GetGenderDefaultEquipData(gender)
				end
				responseData.Data.ModelInfo.BeingId = account.userId
				responseData.Data.ModelInfo.CurrentIslandId = account.islandId
				responseData.Data.CanDiy = responseData.CanDiy and not _ServerEndData.GetAccountData().guide
				-- M.MockCustomClothData(data)
				M.AppendLocalConfig2ServerData(responseData.Data.EquipmentList)
				M.AppendLocalConfig2ServerData(responseData.Data.CustomEquipmentList)
				callback(responseData.Data)
			end
		else
			callback(nil)
			warn("EquipmentService: get response failed response")
		end
	end)
end

---@param itemLists EquipmentListItem[]
function M.AppendLocalConfig2ServerData(itemLists)
	if itemLists == nil then
		return
	end
	local equipmentBaseConfigs = SM:GetConfigService().GetConfig("EquipmentBase")
	for _, value in pairs(itemLists) do
		local config = equipmentBaseConfigs[value.ConfigId]
		if config then
			value.Location = config.EquipmentLocation
			value.IconPath = config.EquipmentIconName
			value.Gender = math.tointeger(config.gender) or 3
		end
	end
end

---@param customIds string[]|table<string>|string
---@param callback function<CustomEquipmentData[]>
function M.GetCustomEquipInfo(customIds, callback)
	if type(customIds) ~= "table" then
		local id = customIds
		customIds = {}
		table.insert(customIds, id)
	end

	local account = SEDService.GetAccountData()
	local reqData = {}
	reqData.OwnerId = account.userId
	reqData.Ids = customIds
	WebRequest.PostJson("BatchGetDIYEquipment", nil, nil, reqData, function (response)
		local result = response
		local data = BatchGetDIYEquipmentResponseData.Decode(response)
		if data then
			if not data.Success then
				warn("EquipmentService GetCustomEquipInfo failed!", result)
				_ToastManager.ShowTip("Info", data.Message, 3)
				callback(nil)
			else
				callback(data.Data)
			end
		else
			callback(nil)
			warn("EquipmentService: get response failed response")
		end
	end)
end

---@param redemptionCode string
---@param callback function<CustomEquipmentData>
function M.GetCustomEquipInfoByCode(redemptionCode, callback)
	local account = SEDService.GetAccountData()
	local reqData = {}
	reqData.OwnerId = account.userId
	reqData.DisPlayId = redemptionCode
	WebRequest.Get("GetSharedDiyEquipment", reqData, nil, function (response)
		local result = response
		local data = RedemptionEquipResponseData.Decode(response)
		if data then
			if not data.Success then
				warn("EquipmentService GetCustomEquipInfoByCode failed!", result)
				_ToastManager.ShowTip("Info", data.Message, 3)
				callback(nil)
			else
				callback(data.Data)
			end
		else
			callback(nil)
			warn("EquipmentService: GetCustomEquipInfoByCode response failed ")
		end
	end)
end

---@param redemptionCode string
---@param callback function<CustomEquipmentData>
function M.SaveCustomEquipInfoByCode(redemptionCode, callback)
	local account = SEDService.GetAccountData()
	local reqData = {}
	reqData.OwnerId = account.userId
	reqData.RedemptionCode = redemptionCode
	WebRequest.PostJson("SaveSharedDiyEquipment", nil, nil, reqData, function (response)
		local result = response
		local data = RedemptionEquipResponseData.Decode(response)
		if data then
			if not data.Success then
				warn("EquipmentService SaveSharedDiyEquipment failed!", result)
				_ToastManager.ShowTip("Info", data.Message, 3)
				callback(nil)
			else
				callback(data.Data)
			end
		else
			callback(nil)
			warn("EquipmentService: SaveSharedDiyEquipment response failed ")
		end
	end)
end

---@param texture2d Texture2D
---@param callback function | nil
function M.UploadTextureForShare(texture2d, callback)
	local result = texture2d:EncodeToPNG()
	WebRequest.PostFileBytes("images", nil, nil, result, function (response)
		local result = response
		local ok, serverData = pcall(cjson.decode, response)
		warn("UploadTextureForShare", response)
		local url = nil
		if ok and serverData then
			url = serverData.url
		else
			warn("EquipmentService Upload failed!", result)
			_ToastManager.ShowTip("Info", "Upload failed", 2)
		end
		if callback then
			callback(url)
		end
	end)
end

function M.MockCustomClothData(data)
	if not data.Data.CustomEquipmentList then
		data.Data.CustomEquipmentList = {}
	end
	local saveRootDir = CS.UnityEngine.Application.persistentDataPath .. "/TestCustomCloth/"
	local Directory = CS.System.IO.Directory
	if not Directory.Exists(saveRootDir) then
		return
	end
	local files = Directory.GetFiles(saveRootDir, "*.data")
	for i = 0, files.Length - 1 do
		local file = files[i]
		local customDataText = CS.System.IO.File.ReadAllText(file)
		local customData = cjson.decode(customDataText)
		table.insert(data.Data.CustomEquipmentList, customData)
	end
end

---@param modelInfo ModelInfo
---@param callback function<boolean|SPerpareData>
function M.SetInfo(modelInfo, callback, dressUpDuration)
	local island = _ServerEndData.GetIslandData()
	if island.isNew then
		M.InitInfo(modelInfo, callback)
		return
	end
	local reqData = {
		BeingId = modelInfo.BeingId,
		FaceId = math.tointeger(modelInfo.FaceId),
		CurrentIslandId = island.id
	}
	---TODO:与服务器协商统一参数
	reqData.AppearanceJson = {}
	for key, value in pairs(modelInfo.AppearanceJson) do
		local variant = value.Variant and value.Variant or ""
		local id = value.Id and value.Id or ""
		local confgId = value.ConfigId
		reqData.AppearanceJson[key] = {
			Id = id,
			Variant = math.tointeger(variant),
			ConfigId = math.tointeger(confgId)
		}
	end
	WebRequest.PostJson("SetPlayerAppearance", nil, nil, reqData, function (response)
		local ok, data = pcall(cjson.decode, response)
		if ok then
			if not data.Success then
				warn("EquipmentService SetInfo failed!", response)
				_ToastManager.ShowTip("Info", data.Message, 3)
			else
				M.Send2NativeSetInfo(modelInfo, dressUpDuration)
			end
			callback(data.Success)
		else
			callback(ok)
			warn("EquipmentService:setinfo decode failed response is ", response)
		end
	end)
end

---@param Id string
---@param isOn boolean
---@param location string
---@param auditStatus integer
---@param callback function<boolean>
function M.DeleteInfo(Id, isOn, location, auditStatus, callback)
	local account = SEDService.GetAccountData()
	local data = {
		OwnerId = account.userId,
		Id = Id,
		IsOn = isOn,
		AuditStatus = auditStatus,
		Location = location
	}
	WebRequest.PostJson("DelDIYEquipment", nil, nil, data, function (response)
		local ok, data = pcall(cjson.decode, response)
		if ok then
			if not data.Success then
				warn("EquipmentService DeleteInfo failed!", response)
				_ToastManager.ShowTip("Info", data.Message, 3)
			end
			callback(data.Success)
		else
			callback(ok)
			warn("EquipmentService: DeleteInfo failed response is nil")
		end
	end)
end

---@param modelInfo ModelInfo
---@param callback function<SPerpareData>
function M.InitInfo(modelInfo, callback)
	local account = SEDService.GetAccountData()
	local reqData = {
		BeingId = account.userId,
		Name = modelInfo.Name,
		IslandId = account.islandId,
		Gender = math.tointeger(modelInfo.Gender),
		FaceId = modelInfo.FaceId
	}
	local equipmentData = modelInfo.AppearanceJson
	for key, value in pairs(equipmentData) do
		local variant = value.Variant or ""
		local id = value.Id or ""
		reqData[key] = id .. '|' .. variant
	end

	WebRequest.Get("PlayerInit", reqData, nil, function (response)
		local data = InitInfoResponseData.Decode(response)
		if data then
			if not data.Success then
				warn("EquipmentService Init Info failed!", response)
				_ToastManager.ShowTip("Info", data.Message, 3)
				callback(nil)
			else
				account.gender = reqData.Gender
				account.name = reqData.Name
				M.Send2NativeInitInfo(modelInfo)
				callback(data.Data)
			end
		else
			callback(nil)
			warn("EquipmentService:  Init Info failed response is nil")
		end
	end)
end

---comment
---@param clothData any
---@param callback function<CustomEquipmentData>
function M.SendDIYClothInfo(clothData, callback)
	local account = SEDService.GetAccountData()
	clothData.OwnerId = account.userId
	WebRequest.PostJson("AddDIYEquipment", nil, nil, clothData, function (response)
		local data = DIYEquipmentResponseData.Decode(response)
		if data then
			if not data.Success then
				warn("EquipmentService SendDIYClothInfo failed!", response)
				_ToastManager.ShowTip("Info", data.Message, 3)
				callback(nil)
			else
				warn("EquipmentService SendDIYClothInfo Success", response)
				callback(data.Data)
			end
		else
			callback(nil)
			warn("EquipmentService:SendDIYClothInfo decode failed response is ", response)
		end
	end)
end

---@param callback function<CustomEquipmentData>
function M.UpdateDIYClothInfo(clothData, callback)
	local account = SEDService.GetAccountData()
	clothData.OwnerId = account.userId

	WebRequest.PostJson("UpdateDiyEquipment", nil, nil, clothData, function (response)
		local data = DIYEquipmentResponseData.Decode(response)
		if data then
			if not data.Success then
				warn("EquipmentService UpdateDIYClothInfo failed!", response)
				_ToastManager.ShowTip("Info", data.Message, 3)
				callback(nil)
			else
				warn("EquipmentService UpdateDIYClothInfo Success", response)
				callback(data.Data)
			end
		else
			callback(nil)
			warn("EquipmentService:UpdateDIYClothInfo decode failed response is ", response)
		end
	end)
end

---@param customEquipmentData CustomEquipmentData
---@param callback function<boolean>
function M.SubmitDIYClothInfo(customEquipmentData, callback)
	local account = SEDService.GetAccountData()
	local clothData = {
		OwnerId = account.userId,
		Id = customEquipmentData.Id,
		ItemName = customEquipmentData.ItemName,
		DisplayId = customEquipmentData.DisplayId,
		CreateUserName = customEquipmentData.CreateUserName,
		DressPictureUrl = customEquipmentData.DressPictureUrl
	}
	WebRequest.PostJson("SubmitDiyEquipment", nil, nil, clothData, function (response)
		local ok, data = pcall(cjson.decode, response)
		if ok then
			if not data.Success then
				warn("EquipmentService SubmitDIYClothInfo failed!", response)
				_ToastManager.ShowTip("Info", data.Message, 3)
				callback(false)
			else
				warn("EquipmentService SubmitDIYClothInfo Success")
				callback(data.Success)
			end
		else
			callback(false)
			warn("EquipmentService:SubmitDIYClothInfo decode failed response is ", response)
		end
	end)
end

---@param modelInfo ModelInfo
function M.Send2NativeInitInfo(modelInfo)
	local initUserInfoMetaData = modelInfo:Convert2NativeInfo()
	local account = SEDService.GetAccountData()
	initUserInfoMetaData.nick_name = account.name
	local gender = "男"
	if account.gender == 1 then
		gender = "男"
	elseif account.gender == 2 then
		gender = "女"
	end
	initUserInfoMetaData.gender = gender
	SM.GetNativeService().Track("InitSetUserInfo", initUserInfoMetaData)
end

---@param modelInfo ModelInfo
---@param dressUpDuration any
function M.Send2NativeSetInfo(modelInfo, dressUpDuration)
	local userInfoMetaData = modelInfo:Convert2NativeInfo()
	userInfoMetaData["$event_duration"] = dressUpDuration
	SM.GetNativeService().Track("UserDressUpClick", userInfoMetaData)
end

function M:VerifyName(name, callback)
	WebRequest.Get("VerifyName", { name = name }, nil, function (response)
		local ok, data = pcall(cjson.decode, response)
		callback(ok and data)
	end)
end

function M:VerifyEquipName(name, callback)
	self:VerifyName(name, callback)
end

function M.LoadPlayer(faceConfig, path, callback)
	local state = _APP:GetCurrentState()
	local loader = state:GetStateLoader()
	if loadContext then
		loadContext.cancel = true
	end
	loadContext = loader:LoadGameObjectAsync(path, function (go)
		loadContext = nil
		Player = go
		M.GetFaceDataJson(faceConfig.facedata, function (text)
			if callback then
				callback(go, text)
			end
		end)
	end, true)
end

function M.ReadFile(path, callback)
	local state = _APP:GetCurrentState()
	local loader = state:GetStateLoader()
	loader:LoadTextAsync(path, function (ref)
		local text = ref:GetTextAsset().text
		callback(text)
		ref:Dispose()
	end)
end

function M.SaveCustomClothPoster(bytes)
	local name = "CustomClothPoster" .. os.date('%Y.%m.%d', os.time())
	local saveRootDir = CS.UnityEngine.Application.persistentDataPath .. "/Poster/"
	local path = saveRootDir .. name .. ".png";
	local Directory = CS.System.IO.Directory
	if not Directory.Exists(saveRootDir) then
		Directory.CreateDirectory(saveRootDir);
	end
	CS.System.IO.File.WriteAllBytes(path, bytes)
	local NativeService = SM.GetService(SM.SERVICE_TYPE.NATIVE)
	NativeService.SavePhoto(path)
end

function M.ClearPlayer()
	if Player then
		CS.Extend.Asset.AssetService.Recycle(Player)
		Player = nil
	end
end

function M.clear()
	active = false
	if loadContext then
		loadContext.cancel = true
		loadContext = nil
	end
	M.ClearPlayer()
	M.EquipmentBaseConfigs = {}
	M.FaceCreationConfigs = {}
end

return M
