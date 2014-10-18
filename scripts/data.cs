datablock ParticleData(ReactorIceParticle)
{
	dragCoefficient = 3;
	gravityCoefficient = 0.25;
	inheritedVelFactor = 0.5;
	constantAcceleration = 0;
	lifetimeMS = 2000;
	lifetimeVarianceMS = 500;
	textureName = "base/data/particles/cloud";
	spinSpeed = 10;
	spinRandomMin = -5;
	spinRandomMax = 5;
	colors[0] = "0.6 0.6 0.6 0.3";
	colors[1] = "0.6 0.6 0.6 0.2";
	colors[2] = "0.6 0.6 0.6 0.1";
	sizes[0] = 1;
	sizes[1] = 0.75;
	sizes[2] = 0.5;
	times[0] = 0.0;
	times[1] = 0.5;
	times[2] = 1.0;
	useInvAlpha = false;
};

datablock ParticleEmitterData(ReactorIceEmitter)
{
	uiName = "Reactor Ice";
	lifetimeMS = 3000;
	ejectionPeriodMS = 50;
	periodVarianceMS = 10;
	ejectonVelocity = 10;
	velocityVariance = 0;
	ejectionOffset = 0;
	thetaMin = 0;
	thetaMax = 0;
	phiReferenceVel = 0;
	phiVariance = 0;
	overrideAdvance = false;
	useEmitterColors = false;
	orientParticles = false;
	particles = "ReactorIceParticle";
};