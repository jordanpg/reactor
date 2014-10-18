//UI Helper Functions
//Author: ottosparks
//makes it easier to poop stuff onto some guy's screen idk

function getTextProgressBar(%bars, %progress, %symbOn, %symbOff)
{
	if(%onBars > 1)
	{
		%onBars /= 100;
		if(%onBars > 1)
			%onBars = 1;
	}

	%onBars = %bars * %progress;

	for(%i = 0; %i < %bars; %i++)
	{
		if(%i < %onBars)
			%str = %str @ %symbOn;
		else
			%str = %str @ %symbOff;
	}

	return %str;
}

function GameConnection::progress_bottom(%this, %time, %bars, %progress, %symbOn, %symbOff, %pre, %post, %i)
{
	%str = %pre @ getTextProgressBar(%bars, %progress, %symbOn, %symbOff) @ %post;
	%this.bottomPrint(%str, %time, %i);
}