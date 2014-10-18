//SimObject::call & SimGroup::chainMethodCall
//Created by Greek2Me on the Blockland Forums
//http://forum.blockland.us/index.php?topic=231470.0

$ChainBatchSize = 100;
$ChainTimeOut = 10;

function SimGroup::chainMethodCall(%this,%method,%v0,%v1,%v2,%v3,%v4,%v5,%v6,%v7,%v8,%v9,%v10,%v11,%v12,%v13,%v14,%v15,%v16,%v17)
{
	cancel(%this.chain_schedule);
	%batch = (%this.chain_batchSize $= "" ? $ChainBatchSize : %this.chain_batchSize);
	%index = (%this.chain_index $= "" ? 0 : %this.chain_index);
	%count = %this.getCount();
	%endIndex = (%index + %batch > %count ? %count : %index + %batch);

	for(%i = %index; %i < %endIndex; %i ++)
	{
		%obj = %this.getObject(%i);
		%obj.call(%method,%v0,%v1,%v2,%v3,%v4,%v5,%v6,%v7,%v8,%v9,%v10,%v11,%v12,%v13,%v14,%v15,%v16,%v17);
	}
	%this.chain_index = %index + %batch;
	if(%this.chain_index >= %count)
	{
		if(isFunction(%this,%this.chain_callback))
			%this.call(%this.chain_callback);
		%this.chain_index = "";
		%this.chain_batchSize = "";
		%this.chain_timeOut = "";
		%this.chain_callback = "";
	}
	else
	{
		%time = (%this.chain_timeOut $= "" ? $ChainTimeOut : %this.chain_timeOut);
		%this.chain_schedule = %this.schedule(%time,"chainMethodCall",%method,%v0,%v1,%v2,%v3,%v4,%v5,%v6,%v7,%v8,%v9,%v10,%v11,%v12,%v13,%v14,%v15,%v16,%v17);
	}
}

function SimObject::call(%this,%method,%v0,%v1,%v2,%v3,%v4,%v5,%v6,%v7,%v8,%v9,%v10,%v11,%v12,%v13,%v14,%v15,%v16,%v17)
{
	%lastNull = -1;
	for(%i = 0; %i < 18; %i ++)
	{
		%a = %v[%i];
		if(%a $= "")
		{
			if(%lastNull < 0)
				%lastNull = %i;
			continue;
		}
		else
		{
			if(%lastNull >= 0)
			{
				for(%e = %lastNull; %e < %i; %e ++)
				{
					if(%args !$= "")
						%args = %args @ ",";
					%args = %args @ "\"\"";
				}
				%lastNull = -1;
			}
			if(%args !$= "")
				%args = %args @ ",";
			%args = %args @ "\"" @ %a @ "\"";
		}
	}

	eval(%this @ "." @ %method @ "(" @ %args @ ");");
}

//Misc. Dependency Methods by ottosparks
//Do a raycast to see what the player is looking at.
function Player::eyeCast(%this, %r, %t)
{
	%eye = %this.getEyePoint();
	%vec = VectorNormalize(%this.getEyeVector());
	%end = VectorAdd(%eye, VectorScale(%vec, %r));
	return containerRayCast(%eye, %end, %t);
}

//Get the normal from a raycast.
function getRayNormal(%ray)
{
	%nX = mFloatLength(getWord(%ray, 4), 0);
	%nY = mFloatLength(getWord(%ray, 5), 0);
	%nZ = mFloor(getWord(%ray, 6));
	return %nX SPC %nY SPC %nZ;
}