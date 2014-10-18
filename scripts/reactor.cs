//Main reactor functionality (DEPRECATED; SEE reactor2.cs)
//Author: ottosparks
//Controls the reactor and most subsystems
$Reactor::CoolantFactor = 500;
$Reactor::CoolantRating = 5;
$Reactor::CoolantVariation = 1.5;
$Reactor::CoolantFlowStep = 5;
$Reactor::CoolantBurnRate = 0.5;
//coolant doesn't use a heat factor

$Reactor::WaterFactor = 5000;
$Reactor::WaterRating = 0.5;
$Reactor::WaterVariation = 0.25;
$Reactor::WaterFlowStep = 10;
$Reactor::WaterBurnRate = 5;
$Reactor::WaterHeatFactor = 0.5;

$Reactor::TickPeriodMS = 1000;
$Reactor::CoreHeatFactor = 10;
$Reactor::CoreRadiationFactor = 0.05;

$Reactor::CoreInstabilityFactor0 = 10;
$Reactor::CoreInstabilityFactor1 = 25;
$Reactor::CoreInstabilityChance0 = 0.25;
$Reactor::CoreInstabilityChance1 = 0.125;

$Reactor::StabilityScale = (1 / 3);
$Reactor::ScaleInst = 5;
$Reactor::ScaleInstCh = 0.05;
$Reactor::ScaleCool = 90;
$Reactor::ScaleCoolBurn = 0.1;
$Reactor::ScaleWater = 900;
$Reactor::ScaleWaterBurn = 1;

function MinigameSO::resetReactor(%this)
{
	if(isObject(%this.reactor))
		%this.reactor.delete();

	%this.reactor = new ScriptObject(ReactorSO)
					{
						coolant = $Reactor::CoolantFactor;
						coolantFlow = 0;

						water = $Reactor::WaterFactor;
						waterFlow = 0;

						heat = 0;

						power = 100;
						monitors = 0;
					};

	return %this.reactor;
}

function ReactorSO::processTick(%this)
{
	%radiation = %this.heat * $Reactor::CoreRadiationFactor;
	%this.heat += $Reactor::CoreHeatFactor;

	if(%this.coolantFlow > 0)
	{
		%coolantBurn = %this.heat * $Reactor::CoolantBurnRate;
		%coolant = %this.coolantFlow - %coolantBurn;
		if(%coolant < 0)
		{
			%coolantBurn -= %this.waterFlow;
			%coolant = 0;
		}

		%coolantEffect = %coolant * ($Reactor::CoolantRating + getRandom(-$Reactor::CoolantVariation, $Reactor::CoolantVariation));
	}

	if(%this.waterFlow > 0)
	{
		%waterBurn = %this.heat * $Reactor::WaterBurnRate;
		%water = %this.waterFlow - %waterBurn;
		if(%water < 0)
		{
			%waterBurn -= %this.waterFlow;
			%water = 0;
		}
	}

	%waterEffect = %water * ($Reactor::WaterRating + (getRandom(-$Reactor::WaterVariation, $Reactor::WaterVariation)));
	%steamHeat = %waterBurn * $Reactor::WaterHeatFactor;

	%this.heat -= (%radiation + %coolantEffect + %waterEffect);
	if(%this.heat < 0)
		%this.heat = 0;

	if(%this.debug)
	{
		echo("REACTOR TICK:");
		echo("--radiation" SPC %radiation);
		echo("--coolant");
		echo("   --burn" SPC %coolantBurn);
		echo("   --amt" SPC %coolant);
		echo("   --effect" SPC %coolantEffect);
		echo("--water");
		echo("   --burn" SPC %waterBurn);
		echo("   --amt" SPC %water);
		echo("   --effect" SPC %waterEffect);
		echo("--steam");
		echo("   --heat" SPC %steamHeat);
		echo(%this.heat);
	}
}

function ReactorSO::tick(%this)
{
	cancel(%this.tick);

	%this.processTick();

	%this.tick = %this.schedule($Reactor::TickPeriodMS, tick);
}