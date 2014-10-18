//Bucketeer Job Functionality
//Author: ottosparks
//shovel my ice you dirty little peasant
$Reactor::Bucketeer::BucketCapacity = 50; //Maximum units of ice an ordinary bucket can hold
$Reactor::Bucketeer::BucketScoopSize = 0.2; //Percentage of maximum capacity that an ordinary bucket will fill per scoop
$Reactor::Bucketeer::FillSpeed = 1.5; //Time in seconds needed to scoop with an ordinary bucket
$Reactor::Bucketeer::BucketDumpSize = 0.4; //Percentage of maximum capacity that an ordinary bucket will empty per dump
$Reactor::Bucketeer::FineFillMod = 0.5; //Fill modifier for fine filling
$Reactor::Bucketeer::FineFillSpeedMod = 1.125; //Speed modifier for fine filling
$Reactor::Bucketeer::FineDumpMod = 0.5; //Dump modifier for fine dumping
$Reactor::Bucketeer::FineDumpSpeedMod = 1.3; //Speed modifier for fine dumping
$Reactor::Bucketeer::DumpSpeed = 0.5; //Time in seconds needed to dump with an ordinary bucket
$Reactor::Bucketeer::UpdateSpeed = 33; 	//[TECHNICAL] Speed of the update tick.
										//Setting this to a lower number means that the UI will update more smoothly and fill times will be more precise.
										//Low numbers may cause some performance issues in certain cases.
$Reactor::Bucketeer::UIFillBars = 50; //[TECHNICAL] Number of bars on the scoop progress bar.
									 //This will not affect fill speed; this will change how many bars will appear in the progress bar bottom print
$Reactor::Bucketeer::UIOnBar = "\c6|";	//[TECHNICAL] Symbols representing portions of the progress bar that are 'on' and 'off'
$Reactor::Bucketeer::UIOffBar = "\c7|";	//This will change how the progress bar looks. The default progress bar looks like this:
										//	|||||||||||||||-----------------------------------
$Reactor::Bucketeer::IceDisplayUnit = "cm^3";
$Reactor::Bucketeer::BucketRange = 5;	//[TECHNICAL] Range of bucket raycast
										//Increase to allow scoop and empty at longer ranges.
$Reactor::Bucketeer::FillSourceName = "REACTOR_ICE";		//[TECHNICAL] Name of bricks that are part of ice interaction
$Reactor::Bucketeer::EmptySourceName = "REACTOR_TROUGH";	//Fill sources allow players to extract ice.
															//Empty sources will allow players to dump ice.


//fxDTSBrick Interactivity
function fxDTSBrick::bucketeer_isValidIceSource(%this)
{
	return (isObject(%this) && striPos(%this.getName(), $Reactor::Bucketeer::FillSourceName) != -1);
}

function fxDTSBrick::bucketeer_isValidDumpSource(%this)
{
	return (isObject(%this) && striPos(%this.getName(), $Reactor::Bucketeer::EmptySourceName) != -1);
}

//Variable access methods
function Player::bucketeer_getBucketSize(%this)
{
	if(!%this.client.bucketeer_hasBucketUpgrade)
		return $Reactor::Bucketeer::BucketCapacity;
	return %this.client.bucketeer_bucketUpgrade;
}

function Player::bucketeer_getScoopSize(%this)
{
	if(!%this.client.bucketeer_hasScoopUpgrade)
		return $Reactor::Bucketeer::BucketScoopSize;
	return %this.client.bucketeer_scoopUpgrade;
}

function Player::bucketeer_getFillSpeed(%this)
{
	if(!%this.client.bucketeer_hasScoopSpeedUpgrade)
		%speed = $Reactor::Bucketeer::FillSpeed;
	else
		%speed = %this.client.bucketeer_scoopSpeedUpgrade;

	return %speed * (%this.bucketeer_fineMode ? $Reactor::Bucketeer::FineFillSpeedMod : 1);
}

function Player::bucketeer_getScoop(%this)
{
	return (%this.bucketeer_getBucketSize() * %this.bucketeer_getScoopSize()) * (%this.bucketeer_fineMode ? $Reactor::Bucketeer::FineFillMod : 1);
}

function Player::bucketeer_getDumpSize(%this)
{
	if(!%this.client.bucketeer_hasDumpUpgrade)
		return $Reactor::Bucketeer::BucketDumpSize;
	return %this.client.bucketeer_DumpUpgrade;
}

function Player::bucketeer_getDumpSpeed(%this)
{
	if(!%this.client.bucketeer_hasDumpSpeedUpgrade)
		%speed = $Reactor::Bucketeer::DumpSpeed;
	else
		%speed = %this.client.bucketeer_dumpSpeedUpgrade;

	return %speed * (%this.bucketeer_fineMode ? $Reactor::Bucketeer::FineDumpSpeedMod : 1); 
}

function Player::bucketeer_getDump(%this)
{
	return (%this.bucketeer_getBucketSize() * %this.bucketeer_getDumpSize()) * (%this.bucketeer_fineMode ? $Reactor::Bucketeer::FineDumpMod : 1);
}


//Game update methods
function GameConnection::bucketeer_displayUI(%this)
{
	if(!isObject(%this.player))
		return;

	%msg = "<just:left>\c3Ice\c6:" SPC mFloatLength(%this.player.bucketeer_ice, 2) @ $Reactor::Bucketeer::IceDisplayUnit;
	%msg = %msg @ "<just:right>\c2Bucketeer \n";

	if(%this.player.bucketeer_scoopStart && %this.player.bucketeer_ice < %this.player.bucketeer_getBucketSize() && %this.player.bucketeer_scoopSeenValidBrick)
	{
		%speed = %this.player.bucketeer_getFillSpeed();
		%diff = $Sim::Time - %this.player.bucketeer_scoopStart;
		%prog = %diff / %speed;

		%msg = %msg @ "<just:center>" @ getTextProgressBar($Reactor::Bucketeer::UIFillBars, %prog, $Reactor::Bucketeer::UIOnBar, $Reactor::Bucketeer::UIOffBar);
	}
	else if(%this.player.bucketeer_dumpStart && %this.player.bucketeer_ice > 0 && %this.player.bucketeer_dumpSeenValidBrick)
	{
		%speed = %this.player.bucketeer_getDumpSpeed();
		%diff = $Sim::Time - %this.player.bucketeer_dumpStart;
		%prog = %diff / %speed;

		%msg = %msg @ "<just:center>" @ getTextProgressBar($Reactor::Bucketeer::UIFillBars, %prog, $Reactor::Bucketeer::UIOffBar, $Reactor::Bucketeer::UIOnBar);
	}

	%msg = %msg NL "<just:center>\c3Left-Click on a source to fill; Right-Click on a trough to empty";
	if(%this.player.bucketeer_fineMode)
		%msg = %msg NL "\c4FINE";

	%this.bottomPrint(%msg, 1, 1);
}

function Player::bucketeer_update(%this)
{
	if(isEventPending(%this.bucketeer_update))
		cancel(%this.bucketeer_update);

	if(!%this.bucketeer_hasBucket)
		return;

	if(isObject(%this.client))
		%this.client.bucketeer_displayUI();

	if(%this.bucketeer_scoopStart)
		%this.bucketeer_scoopUpdate();
	else if(%this.bucketeer_dumpStart)
		%this.bucketeer_dumpUpdate();

	%this.bucketeer_update = %this.schedule($Reactor::Bucketeer::UpdateSpeed, bucketeer_update);
}


//Scooping
function Player::bucketeer_scoopBegin(%this)
{
	if(!%this.bucketeer_hasBucket || %this.bucketeer_ice == %this.bucketeer_getBucketSize())
		return;

	%this.bucketeer_scoopStart = $Sim::Time;
	%this.bucketeer_scoopSeenValidBrick = true;
}

function Player::bucketeer_scoopUpdate(%this)
{
	if(!%this.bucketeer_hasBucket)
		return;

	if(!%this.bucketeer_scoopStart)
		%this.bucketeer_scoopStart = $Sim::Time;

	%eyeCast = %this.eyeCast($Reactor::Bucketeer::BucketRange, $TypeMasks::FxBrickObjectType);
	%obj = firstWord(%eyeCast);
	if(!isObject(%obj) || !%obj.bucketeer_isValidIceSource())
	{
		%this.bucketeer_scoopReset();
		return;
	}

	%this.bucketeer_scoopSeenValidBrick = true;

	%speed = %this.bucketeer_getFillSpeed();
	if((%diff = $Sim::Time - %this.bucketeer_scoopStart) >= %speed)
	{
		%this.bucketeer_ice += %this.bucketeer_getScoop();
		%capacity = %this.bucketeer_getBucketSize();
		if(%this.bucketeer_ice > %capacity)
		{
			%this.bucketeer_ice = %capacity;
			%this.bucketeer_scoopEnd();
		}
		else
		{
			%this.bucketeer_scoopReset();
		}
	}
}

function Player::bucketeer_scoopEnd(%this)
{
	%this.bucketeer_scoopStart = "";
}

function Player::bucketeer_scoopReset(%this)
{
	%this.bucketeer_scoopStart = $Sim::Time;
	%this.bucketeer_scoopSeenValidBrick = false;
}


//Dumping
function Player::bucketeer_dumpBegin(%this)
{
	if(!%this.bucketeer_hasBucket || %this.bucketeer_ice == 0)
		return;

	%this.bucketeer_DumpStart = $Sim::Time;
	%this.bucketeer_dumpSeenValidBrick = true;
}

function Player::bucketeer_dumpUpdate(%this)
{
	if(!%this.bucketeer_hasBucket)
		return;

	if(!%this.bucketeer_DumpStart)
		%this.bucketeer_DumpStart = $Sim::Time;

	%eyeCast = %this.eyeCast($Reactor::Bucketeer::BucketRange, $TypeMasks::FxBrickObjectType);
	%obj = firstWord(%eyeCast);
	if(!isObject(%obj) || !%obj.bucketeer_isValidDumpSource())
	{
		%this.bucketeer_DumpReset();
		return;
	}

	%this.bucketeer_scoopSeenValidBrick = true;

	%speed = %this.bucketeer_getDumpSpeed();
	if((%diff = $Sim::Time - %this.bucketeer_DumpStart) >= %speed)
	{
		%this.bucketeer_ice -= %this.bucketeer_getDump();
		if(%this.bucketeer_ice < 0)
		{
			%this.bucketeer_ice = 0;
			%this.bucketeer_DumpEnd();
		}
		else
		{
			%this.bucketeer_dumpReset();
		}
	}
}

function Player::bucketeer_dumpEnd(%this)
{
	%this.bucketeer_DumpStart = "";
}

function Player::bucketeer_dumpReset(%this)
{
	%this.bucketeer_DumpStart = $Sim::Time;
	%this.bucketeer_scoopSeenValidBrick = false;
}

//Package
package Reactor_Bucketeer
{
	function Armor::onTrigger(%this, %obj, %slot, %val)
	{
		parent::onTrigger(%this, %obj, %slot, %val);

		if(!%obj.bucketeer_hasBucket)
			return;

		if(!isEventPending(%obj.bucketeer_update))
			%obj.bucketeer_update();

		if(%obj.bucketeer_ice $= "")
			%obj.bucketeer_ice = 0;

		%eyeCast = %obj.eyeCast($Reactor::Bucketeer::BucketRange, $TypeMasks::FxBrickObjectType);
		%source = firstWord(%eyeCast);

		if(!isObject(%source))
		{
			if(%slot $= 4 && %val && %obj.isCrouched())
				%obj.bucketeer_fineMode = !%obj.bucketeer_fineMode;
			return;
		}

		if(%slot $= 0 && %val && !%obj.bucketeer_dumpStart && %source.bucketeer_isValidIceSource())
			%obj.bucketeer_scoopBegin();
		else
			%obj.bucketeer_scoopEnd();

		if(%slot $= 4 && %val && !%obj.bucketeer_scoopStart && %source.bucketeer_isValidDumpSource())
			%obj.bucketeer_dumpBegin();
		else
			%obj.bucketeer_dumpEnd();

	}
};
activatePackage(Reactor_Bucketeer);