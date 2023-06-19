local StateBase = require("Game.State.StateBase")
---@class Game.State.MockLoginState: Game.State.StateBase
local M = class(StateBase)
local SM = require("ServiceManager")

-- abstract
function M:Enter()
	self.ready = false
	StateBase.Enter(self)
	local sceneLoadManager = CS.Extend.Services.SceneLoadManager.Get()
	local file = "Assets/Scenes/MockLogin.unity"--SM.GetConfigService().PathForFile("MockLogin.unity")
	sceneLoadManager:LoadSceneAsync(file, false, function()
		self.ready = true
		local UIService = SM.GetService(SM.SERVICE_TYPE.UI)
		UIService.Show("MockLogin")
		UIService.AfterSceneLoaded()
	end)
end

function M:Update()
end

function M:SocketStatusChanged()
end

function M:GetStateName()
	return "Login"
end

-- abstract
function M:Exit()
	local sceneLoadManager = CS.Extend.Services.SceneLoadManager.Get()
	local file = "Assets/Scenes/MockLogin.unity"--SM.GetConfigService().PathForFile("MockLogin.unity")
	sceneLoadManager:UnloadScene(file)
	SM.GetUIService().Hide("MockLogin")
end

function M:GetIsReady()
	return self.ready
end

function M:GetStateType()
end

return M