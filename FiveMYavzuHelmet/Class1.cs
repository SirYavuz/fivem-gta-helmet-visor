using System;
using System.Collections.Generic;
using System.Threading;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace FiveMYavzuHelmet
{
    public class Class1 : BaseScript
    {
        public Class1()
        {
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);
        }

        private void OnClientResourceStart(string resourceName)
        {
            if (API.GetCurrentResourceName() != resourceName) return;

            API.RegisterCommand("visor", new Action(CheckHelmet), false);
            API.RegisterKeyMapping("visor", "Change Helmet Visor", "keyboard", "");
            Thread.Sleep(1000);
        }

        private async void CheckHelmet()
        {
            var component = Function.Call<int>(Hash.GET_PED_PROP_INDEX, Game.PlayerPed.Handle, 0);      // helmet index
            var texture = Function.Call<int>(Hash.GET_PED_PROP_TEXTURE_INDEX, Game.PlayerPed.Handle, 0); // texture
            var compHash = Function.Call<uint>(Hash.GET_HASH_NAME_FOR_PROP, Game.PlayerPed.Handle, 0, component, texture); // prop combination hash

            if (Function.Call<int>((Hash)0xD40AAC51E8E4C663, compHash) > 0) // helmet has visor.
            {
                var newHelmet = component;
                var newHelmetTexture = texture;

                var newHelmetData = Game.GetAltPropVariationData(Game.PlayerPed.Handle, 0);

                if (newHelmetData != null && newHelmetData.Length > 0)
                {
                    newHelmet = newHelmetData[0].altPropVariationIndex;
                    newHelmetTexture = newHelmetData[0].altPropVariationTexture;
                }

                TriggerEvent("yavzu-shits:altprops", newHelmet, newHelmetTexture, newHelmetData);

                var animName = component < newHelmet ? "visor_up" : "visor_down";
                if (Game.PlayerPed.Model == PedHash.FreemodeFemale01)
                {
                    if ((component == 66 || component == 81) && (component >= 115 && component <= 118))
                    {
                        animName = component > newHelmet ? "visor_up" : "visor_down";
                    }
                    if (component == 66 || component == 81)
                    {
                        animName = component > newHelmet ? "visor_up" : "visor_down";
                    }
                    if (component >= 115 && component <= 118)
                    {
                        animName = component < newHelmet ? "goggles_up" : "goggles_down";
                    }
                }
                else
                {
                    if ((component == 67 || component == 82) && (component >= 116 && component <= 119))
                    {
                        animName = component > newHelmet ? "visor_up" : "visor_down";
                    }
                    if (component == 67 || component == 82)
                    {
                        animName = component > newHelmet ? "visor_up" : "visor_down";
                    }
                    if (component >= 116 && component <= 119)
                    {
                        animName = component < newHelmet ? "goggles_up" : "goggles_down";
                    }
                }

                var animDict = "anim@mp_helmets@on_foot";

                if (API.GetFollowPedCamViewMode() == 4)
                {
                    if (animName.Contains("goggles"))
                    {
                        animName = animName.Replace("goggles", "visor");
                    }
                    animName = "pov_" + animName;
                }
                if (Game.PlayerPed.IsInVehicle())
                {
                    if (animName.Contains("goggles"))
                    {
                        API.ClearAllHelpMessages();
                        API.BeginTextCommandDisplayHelp("string");
                        API.AddTextComponentSubstringPlayerName("You can not toggle your goggles while in a vehicle.");
                        API.EndTextCommandDisplayHelp(0, false, true, 6000);
                        return;
                    }

                    var veh = API.GetVehiclePedIsIn(Game.PlayerPed.Handle, false);
                    if (veh != null)
                    {
                        var vehClass = API.GetVehicleClass(veh);
                        if (vehClass == 8 || vehClass == 13)
                        {
                            animDict = "anim@mp_helmets@on_bike@sports";

                        }
                    }
                }

                if (!API.HasAnimDictLoaded(animDict))
                {
                    API.RequestAnimDict(animDict);
                    while (!API.HasAnimDictLoaded(animDict))
                    {
                        await Delay(0);
                    }
                }

                if (animName.StartsWith("pov_") && animDict != "anim@mp_helmets@on_foot")
                {
                    animName = animName.Substring(4);
                }

                API.ClearPedTasks(Game.PlayerPed.Handle);
                API.TaskPlayAnim(Game.PlayerPed.Handle, animDict, animName, 8.0f, 1.0f, -1, 48, 0.0f, false, false, false);
                
                var timeoutTimer = API.GetGameTimer();
                while (API.GetEntityAnimCurrentTime(Game.PlayerPed.Handle, animDict, animName) <= 0.0f)
                {
                    if (API.GetGameTimer() - timeoutTimer > 1000)
                    {
                        API.ClearPedTasks(Game.PlayerPed.Handle);
                        Debug.WriteLine("[WARNING] Waiting for animation to start took too long. Preventing hanging of function. Dbg: fault in location 1.");
                        return;
                    }
                    await Delay(0);
                }
                timeoutTimer = API.GetGameTimer();
                while (API.GetEntityAnimCurrentTime(Game.PlayerPed.Handle, animDict, animName) > 0.0f)
                {
                    await Delay(0);

                    if (API.GetGameTimer() - timeoutTimer > 3000)
                    {
                        API.ClearPedTasks(Game.PlayerPed.Handle);
                        Debug.WriteLine("[WARNING] Waiting for animation duration took too long. Preventing hanging of function. Dbg: fault in location 2.");
                        return;
                    }
                    if (API.GetEntityAnimCurrentTime(Game.PlayerPed.Handle, animDict, animName) > 0.39f)
                    {
                        API.SetPedPropIndex(Game.PlayerPed.Handle, 0, newHelmet, newHelmetTexture, true);
                    }
                }

                API.ClearPedTasks(Game.PlayerPed.Handle);
                API.RemoveAnimDict(animDict);
            }
        }
    }
}
