//Reactor stuff v2
//Author: ottosparks
//This was revised to be about one million percent less asinine.
//Current version as of October 5, 2014
//GENERAL GAMEMODE SETTINGS
$Reactor::PhaseTimer = 200;
$Reactor::TickTimer = 0;
//RELATING TO CORE HEATING
$Reactor::CoolantTransferCoeff 		=	0.5;	//Transfer coefficients dictate how effective fluids will be at removing heat from the core.
$Reactor::WaterTransferCoeff 		=	0.01;	//This is applied to the difference in temperatures to take heat from the reactor and add it to the fluid.
$Reactor::CoreTransferCoeff			=	0.05;
$Reactor::BaseCoolantTemperature	=	0;		//Base temperature values are simply the temperatures at which the fluids begin.
$Reactor::BaseWaterTemperature		=	50;
$Reactor::BaseHeatingRate 			=	0.6; 	//Heat accumulated per tick
$Reactor::SingularityActivityRate 	=	0.05; 	//Activity % increase per unit of heat. This percentage will be applied to the base heating rate and added to the overall effect.
//RELATING TO POWER
$Reactor::SteamGenerationCoeff		=	0.25;	//Volume of steam produced per unit of heat difference
$Reactor::SteamPowerRate			=	1;		//Amount of power generated per unit of steam volume
$Reactor::TurbineProcessingRate		=	20;		//Volume of steam that can be handled by one turbine each tick.
//RELATING TO FLUID COOLING
$Reactor::IceTemperature			=	-10;
$Reactor::IceEffectiveness			=	0.01;	//Percantage of temperature difference removed per unit of ice
$Reactor::IceMeltRate				=	0.1;	//Amount of ice removed per unit of heat transfer.

function MinigameSO::reactorReset(%this)
{
	if(isObject(%this.reactor))
		%this.reactor.delete();

	%so = new ScriptObject(ReactorSO)
			{
				coolantTemp 	= $Reactor::BaseCoolantTemperature;
				waterTemp 		= $Reactor::BaseWaterTemperature;
				heat 			= 0;

				steam 			= 0;
				power 			= 0;
				turbines 		= 1;

				iceCoolant 		= 0;
				iceWater		= 0;
			};
	%this.reactor = %so;

	return %so;
}


function ReactorSO::debug(%this, %msg)
{
	if(%this.talk)
		talk(%msg);
	else
		%this.debug(%msg);
}

function ReactorSO::reactorPhase1(%this)
{
	//REACTOR PHASE ONE: Core Heating Phase
	//In this phase, the reactor heat progresses, taking all direct fluid and heating dynamics into effect.

	//Calculate accumulated heat this tick
	//h += rate of heating + (rate of heating * % of increase due to core activity)
	%hAccum = $Reactor::BaseHeatingRate;
	%this.tick_effSing = %sAccum = ($Reactor::BaseHeatingRate * $Reactor::SingularityActivityRate * %this.heat);

	//Get difference in temperature of fluids and core heat
	%diffCoolant = %this.coolantTemp - %this.heat;
	%diffWater = %this.waterTemp - %this.heat;

	//Calculate the volume of steam generated this tick from the difference of water heat
	%volSteam = %diffWater * $Reactor::SteamGenerationCoeff;

	//Calculate the transfer of heat from the difference in temperature
	%effCoolant = %diffCoolant * (%diffCoolant < 0 ? $Reactor::CoolantTransferCoeff : $Reactor::CoreTransferCoeff);
	%effWater = %diffWater * (%diffWater < 0 ? $Reactor::WaterTransferCoeff : $Reactor::CoreTransferCoeff);

	//Add all the temperature effects together
	%effTotal = %effCoolant + %effWater + %hAccum + %sAccum;

	//Modify the values
	%hBefore = %this.heat;
	%this.heat += (%this.tick_heatGen = %effTotal);
	%cBefore = %this.coolantTemp;
	%this.coolantTemp -= (%this.tick_hEffCoolant = %effCoolant);
	%wBefore = %this.waterTemp;
	%this.waterTemp -= (%this.tick_hEffWater = %effWater);

	%sBefore = %this.steam;
	%this.steam += (%this.tick_steamGen = mAbs(%volSteam));

	if(%this.debug)
	{
		%this.debug("REACTOR PHASE 1" SPC $Sim::Time);
		%this.debug(" +--heat acc." SPC %hAccum SPC "," SPC %sAccum);
		%this.debug(" +--fluids");
		%this.debug("   +--coolant" SPC %diffCoolant SPC "," SPC %effCoolant);
		%this.debug("   +--water" SPC %diffWater SPC "," SPC %effWater);
		%this.debug("   +--total" SPC %effTotal);
		%this.debug(" +--steam" SPC %volSteam);
		%this.debug(" +--heat" SPC %hBefore SPC "->" SPC %this.heat);
		%this.debug(" +--coolant temp" SPC %cBefore SPC "->" SPC %this.coolantTemp);
		%this.debug(" +--water temp" SPC %wBefore SPC "->" SPC %this.waterTemp);
		%this.debug(" +--steam vol" SPC %sBefore SPC "->" SPC %this.steam);
	}

	return true;
}

function ReactorSO::reactorPhase2(%this)
{
	//REACTOR PHASE TWO: Power Generation Phase
	//In this phase, steam is converted into power.

	//Get the maximum amount of steam we can process this tick.
	%maxProcessed = %this.turbines * $Reactor::TurbineProcessingRate;
	//Cap off the actual amount we're processing so that it can never be more than the maximum.
	%this.tick_steamProc = %proc = (%this.steam <= %maxProcessed ? %this.steam : %maxProcessed);

	//Get the amount of power we're generating from the given volume of steam.
	%this.tick_powerGen = %pGenerated = %proc * $Reactor::SteamPowerRate;

	//Add the power.
	%pBefore = %this.power;
	%this.power += %pGenerated;

	//Remove the processed steam from the system.
	%sBefore = %this.steam;
	%this.steam -= %proc;
	if(%this.steam < 0)
		%this.steam = 0;

	if(%this.debug)
	{
		%this.debug("REACTOR PHASE 2" SPC $Sim::Time);
		%this.debug(" +--processed" SPC %proc SPC "/" SPC %maxProcessed);
		%this.debug(" +--power" SPC %pBefore SPC "-" @ %pGenerated @ "->" SPC %this.power);
		%this.debug(" +--steam" SPC %sBefore SPC "->" SPC %this.steam);
	}

	return true;
}

function ReactorSO::reactorPhase3(%this)
{
	//REACTOR PHASE THREE: Fluid Cooling Phase
	//In this phase, the super-ice or w/e is consumed to cool down fluids going into the reactor.

	%diffCoolant = $Reactor::IceTemperature - %this.coolantTemp;
	%diffWater = $Reactor::IceTemperature - %this.coolantTemp;

	%iceTransCoolant = %this.iceCoolant * $Reactor::IceEffectiveness;
	%iceTransWater = %this.iceWater * $Reactor::IceEffectiveness;
	%this.tick_fEffCoolant = %effCoolant = %diffCoolant * (%diffCoolant < 0 ? %iceTransCoolant : $Reactor::CoolantTransferCoeff);
	%this.tick_fEffWater = %effWater = %diffWater * (%diffWater < 0 ? %iceTransWater : $Reactor::WaterTransferCoeff);

	%this.tick_meltCoolant = %meltCoolant = -mFloor(%effCoolant * $Reactor::IceMeltRate);
	%this.tick_meltWater = %meltWater = -mFloor(%effWater * $Reactor::IceMeltRate);

	%cBefore = %this.coolantTemp;
	%this.coolantTemp += %effCoolant;
	%wBefore = %this.waterTemp;
	%this.waterTemp += %effWater;

	%iCBefore = %this.iceCoolant;
	%this.iceCoolant -= %meltCoolant;
	%iWBefore = %this.iceWater;
	%this.iceWater -= %meltWater;

	if(%this.iceCoolant < 0)
		%this.iceCoolant = 0;
	if(%this.iceWater < 0)
		%this.iceWater = 0;

	if(%this.debug)
	{
		%this.debug("REACTOR PHASE 3" SPC $Sim::Time);
		%this.debug(" +--coolant" SPC %cBefore SPC "-(" @ %effCoolant @ ")->" SPC %this.coolantTemp);
		%this.debug(" +--water" SPC %wBefore SPC "-(" @ %effWater @ ")->" SPC %this.waterTemp);
		%this.debug(" +--iceCoolant" SPC %iCBefore SPC "-" @ %meltCoolant @ "->" SPC %this.iceCoolant);
		%this.debug(" +--iceWater" SPC %iWBefore SPC "-" @ %meltWater @ "->" SPC %this.iceWater);
	}

	return true;
}

function ReactorSO::reactorPhase4(%this)
{
	//REACTOR PHASE 4: Subsystem Phase
	//This will be where subsystems like monitors are handled. I don't have anything to put here right now, though.

	if(%this.debug)
	{
		%this.debug("REACTOR PHASE 4" SPC $Sim::Time);
	}

	return -1;
}

function ReactorSO::reactorPhase0(%this)
{
	//REACTOR PHASE 0: Cleanup Phase
	//This is where some temporary values are reset and some other butt happens.

	%this.tick_currPhase = 0;

	%this.tick_meltWater = "";
	%this.tick_meltCoolant = "";
	%this.tick_powerGen = "";
	%this.tick_steamProc = "";
	%this.tick_steamGen = "";
	%this.tick_heatGen = "";
	%this.tick_effSing = "";
	%this.tick_hEffCoolant = "";
	%this.tick_hEffWater = "";
	%this.tick_fEffCoolant = "";
	%this.tick_fEffWater = "";

	if(%this.debug)
	{
		%this.debug("REACTOR PHASE 0" SPC $Sim::Time);
	}
}

function ReactorSO::startTick(%this)
{
	cancel(%this.tick);
	cancel(%this.phase);

	if(%this.debug)
	{
		%this.debug("REACTOR TICK START" SPC $Sim::Time);
	}

	%this.reactorPhase0();

	%this.phase = %this.schedule($Reactor::PhaseTimer, advancePhase);

	return true;
}

function ReactorSO::advancePhase(%this)
{
	cancel(%this.phase);

	%this.tick_currPhase++;
	if(!isFunction(ReactorSO, (%func = "reactorPhase" @ %this.tick_currPhase)))
	{
		%this.endTick();
		return false;
	}

	%r = %this.call(%func);

	if(%r <= 0)
	{
		%this.endTick();
		return false;
	}

	%this.phase = %this.schedule($Reactor::PhaseTimer, advancePhase);
}

function ReactorSO::endTick(%this)
{
	//probly do some gamemode checks here
	cancel(%this.phase);

	if(%this.debug)
	{
		%this.debug("REACTOR TICK END" SPC $Sim::Time);
		%this.debug("+heat" SPC %this.heat);
		%this.debug("+coolant" SPC %this.coolantTemp);
		%this.debug("+water" SPC %this.waterTemp);
		%this.debug("+iceC" SPC %this.iceCoolant);
		%this.debug("+iceW" SPC %this.iceWater);
		%this.debug("+power" SPC %this.power);
		%this.debug("+steam" SPC %this.steam);
	}

	if(!%this.wait)
		%tick = true;
	else
	{
		if(%this.waitForHeat)
		{
			if(%this.heat < %this.waitForHeat)
			{
				$cond_heat++;
				%tick = true;
			}
			else
			{
				%this.debug("Reached heat end in" SPC $cond_heat SPC "ticks, " SPC %this.waitforheat);
				$cond_heat = 0;
				%this.waitForHeat = 0;
			}
		}
		
		if(%this.waitForTick)
		{
			$cond_tick++;
			%tick = true;
			%this.waitForTick--;
			if(%this.waitForTick == 0)
			{
				%this.debug("Reached tick end" SPC $cond_tick);
				$cond_tick = 0;
			}
		}

		if(isFunction(%this.waitForCondition))
		{
			$cond_func++;
			%r = call(%this.waitForCondition, %this);

			if(!%r)
			{
				%this.waitForCondition = "";
				%this.debug("Reached func end in" SPC $cond_func SPC "ticks");
				$cond_func = 0;
			}
			else
				%tick = true;
		}
	}

	if(%tick)
		%this.tick = %this.schedule($Reactor::TickTimer, startTick);
	return true;
}