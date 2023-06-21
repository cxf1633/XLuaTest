local LuaBindingBase = require("base.LuaBindingBase")
local binding = require("mvvm.binding")
local LuaMVVMBindingType = typeof(CS.Extend.LuaMVVM.LuaMVVMBinding)
-- local ConfigService = require "ConfigService"
---@class UI.MockLogin : LuaBinding
local M = class(LuaBindingBase)

function M:start()
	-- local namesTable = {}
	-- local islandsTable = {}
	-- --local islandsTable = {}
	-- local configRows = ConfigService.GetConfig("MockLogin")
	-- for key, value in pairs(configRows) do
	-- 	table.insert(namesTable, { text = key })
	-- 	table.insert(islandsTable, { text = value.islandName })
	-- end

	-- local account = CS.UnityEngine.PlayerPrefs.GetString("Account", namesTable[1].text)
	-- local island = CS.UnityEngine.PlayerPrefs.GetString("Island", islandsTable[1].text)
	-- local nameIndex = table.index_of_predict(namesTable, function(nameElement)
	-- 	return nameElement.text == account
	-- end)
	-- local islandIndex = table.index_of_predict(islandsTable, function(islandElement)
	-- 	return islandElement.text == island
	-- end)
	-- local accountInput = CS.UnityEngine.PlayerPrefs.GetString("AccountInput")
	-- local IslandInpput = CS.UnityEngine.PlayerPrefs.GetString("IslandInpput")
	-- local context = binding.build({
	-- 	data = {
	-- 		acid = accountInput,
	-- 		islandId = IslandInpput,
	-- 		names = namesTable,
	-- 		islands = islandsTable,
	-- 		username = nameIndex - 1,
	-- 		islandName = islandIndex - 1
	-- 	}
	-- })
	-- context:watch("username", function(index)
	-- 	self.context.islandName = index
	-- end)
	-- self.context = context
	-- local mvvm = self.__CSBinding:GetComponent(LuaMVVMBindingType)
	-- mvvm:SetDataContext(context)

	print("MockLogin")
	local context = binding.build({
		data = {
			account = "",
			password = ""
		}
	})
	self.context = context
	local mvvm = self.__CSBinding:GetComponent(LuaMVVMBindingType)
	mvvm:SetDataContext(context)
end

function M:BuildLoginInfo()
	local info = {}
	local islandId, isOwner
	if self.context.acid ~= "" and self.context.islandId ~= "" then
		info.accountUuid = self.context.acid
		info.accessKey = self.context.acid
		info.islandId = self.context.islandId
		islandId = self.context.islandId
		isOwner = "true"
	else
		local configRows = ConfigService.GetConfig("MockLogin")
		local name = self.context.names[self.context.username + 1].text
		local islandName = self.context.islands[self.context.islandName + 1].text
		info = {}
		for k, v in pairs(configRows) do
			if name == k then
				info.accountUuid = v.userId
				info.accessKey = v.userId
				info.islandId = v.islandId
			end
			if islandName == v.islandName then
				islandId = v.islandId
			end
		end
		isOwner = tostring(islandId == info.islandId)
		CS.UnityEngine.PlayerPrefs.SetString("Account", name)
		CS.UnityEngine.PlayerPrefs.SetString("Island", islandName)
	end
	CS.UnityEngine.PlayerPrefs.SetString("AccountInput", self.context.acid)
	CS.UnityEngine.PlayerPrefs.SetString("IslandInpput", self.context.islandId)
	return info, isOwner, islandId
end


function M:OnLoginClicked()
	-- local info, isOwner, islandId = self:BuildLoginInfo()
	-- _APP:UpdateInfo(info)
	-- _APP:Landing({ isNew = "false", isOwner = isOwner, islandId = islandId, qualityLevel = "1" })

	print("OnLoginClicked")
	self.context.account= "nihao"
	self.context.password= "mima"
end


function M:OnGuideClicked()
	local info, isOwner, islandId = self:BuildLoginInfo()
	_APP:UpdateInfo(info)
	_APP:Landing({ isNew = "true", islandId = islandId, isOwner = "true", qualityLevel = "1", sceneName = "BeginnerGuide" })
end

function M:OnShowPanelClicked()
end

function M:OnChangeClothClick()
	local info, isOwner, islandId = self:BuildLoginInfo()
	_APP:UpdateInfo(info)
	_APP:Landing({ isNew = "false", islandId = islandId, isOwner = "true", qualityLevel = "1", sceneName = "ChangeCloth" })
end

function M:OnCoffeeShopClick()
	local CoffeeShopState = require("Game.State.CoffeeShopState")
	_APP:Switch(CoffeeShopState.new())
end

return M
