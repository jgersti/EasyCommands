﻿# Wheel Suspension Block Handler
This Block Handler handles Wheel Suspension (not wheels directly, but still controls them).  It can control most properties of Wheel Suspension including auto steering & propulsion (anyone else thinking remote control rovers??).

* Block Type Keywords: ```wheel```
* Block Type Group Keywords: ```wheels, suspension```

Default Primitive Properties:
* Numeric - Height

Default Directional Properties
* Up - Height
* Down - Height

## "Enabled" Property
* Primitive Type: Bool
* Keywords: ```enable, enabled```
* Inverse Keywords: ```disable, disabled```

Enables or Disables the given block

```
#Enable Block
enable "My Wheel"
set "My Wheel" to enabled
turn on "My Wheel"

#Disable Block
disable "My Wheel"
set "My Wheel" to disabled
turn off "My Wheel"
```

## "Height" Property
* Primitive Type: Numeric
* Keywords: ```height, heights, level, levels```

Get/Sets the Wheel Height, in meters.

```
Print "Wheel Height: " + "My Wheel" height

set "My Wheel" height to 0.5
```

## "Angle" Property
* Primitive Type: Numeric
* Keywords: ```angle, angles, azimuth, azimuths```

Gets/Sets the Wheel steering angle. Values for this property range from -45 (Left) to 45 (Right), with 0 = Straight Forward.

Setting this property to a specific angle will set the maximum steering angle to 100% and then set the steering override such that the wheel angle is the one you requested (between -45 and 45)

```
Print "Steering Angle" + "My Wheel" angle

#Turn left
set "My Wheel" angle to -45

#Straight
set "My Wheel" angle to 0

#Turn Right
set "My Wheel" angle to 45
```

## "Velocity" Property
* Primitive Type: Numeric
* Keywords: ```velocity, velocities, speed, speeds, rate, rates```

Gets/Sets the Propulsion Override (used to drive by itself).  Values for this property are between -1 and 1, with -1 = 100% reverse, 0 = stopped, and 1 = 100% forward.

```
Print "Propulsion Override: " + "My Wheel" velocity

#Forward
set "My Wheel" velocity to 1

#Stop
set "My Wheel" velocity to 0

#Backward
set "My Wheel" velocity to -1
```

## "Speed Limit" Property
* Primitive Type: Numeric
* Keywords: ```speed limit, speed limits```

Gets/Sets the Speed Limit of the wheels in km/h.

```
Print "Speed Limit" + "My Wheel" speed limit

#50 m/s Speed Limit
set "My Wheel" speed limit to 50
```

## "Velocity Override" Property
* Primitive Type: Numeric
* Keywords: ```velocity override, speed override```

Gets/Sets the propulsion override of the given wheel(s), as a value from -1 to 1.

```
Print "Propulsion Override: " + "My Wheel" velocity override

set "My Wheel" velocity override to 0.5
```

## "Steering" Property
* Primitive Type: Bool
* Keywords: ```steer, steering```

Gets/Sets whether steering is enabled on the selected wheel(s).

```
Print "Steering Enabled: " + "My Wheel" steering is on

turn off "My Wheel" steering
```

## "Steering Limit" Property
* Primitive Type: Numeric
* Keywords: ```steering limit, steer limit```

Sets the Maximum Steer Angle (which affects both left & right equally).  Expected values are between 0 - 1 (0 = no steering, 1 = maximum steering)

```
Print "Steering Limit: " + "My Wheel" steering limit

#Only steering turning up to 50%
set "My Wheel" steering limit to 0.5
```

## "Steering Override" Property
* Primitive Type: Numeric
* Keywords: ```steer override, steering override```

Gets/Sets the Steering Override, as a value from -1 to 1

```
Print "Steering Override: " + "My Wheel" steering override

set "My Wheel" steering override to -0.5
```

## "Locked" Property
* Primitive Type: Bool
* Keywords: ```lock, locked, brake, braking, handbrake```
* Inverse Keywords: ```unlock, unlocked```

Gets/Sets whether the wheel responds when you hit the brakes.  This does not turn on the brakes by itself!  To automatically brake you'll need to use a [Cockpit](https://spaceengineers.merlinofmines.com/EasyCommands/blockHandlers/cockpit "Cockpit Handler").

```
if "My Wheel" brake is off
  Print "Someone disconnected the brakes!"

#Wheel will respond to braking
turn on "My Wheel" brake
```

## "Strength" Property
* Primitive Type: Numeric
* Keywords: ```strength, strengths, force, forces, torque, torques```

Gets/Sets the strength of the wheels.  Values are between 0 - 100, with 100 being the highest strength.  Higher strength values keep the wheel suspension higher off the ground under heavy loads.

```
Print "Wheel Strength: " + "My Wheel" strength

#50% strength
set "My Wheel" strength to 50
```

## "Power" Property
* Primitive Type: Numeric
* Keywords: ```power```

Gets/Sets the Wheel Power.  Values are between 0 - 100, with 100 = Max Power.

```
Print "Wheel Power: " + "My Wheel" power

#50% power
set "My Wheel" power to 50
```

## "Ratio" Property
* Primitive Type: Numeric
* Keywords: ```ratio, ratios, percentage, percentages, percent, percents```

Gets/Sets the Wheel's Friction, as a ratio between 0 - 100.  100 = 100% Friction.  Higher friction values will give the wheel more traction.

```
Print "Wheel Friction: " + "My Wheel" ratio

#Set Friction to 50%
set "My Wheel" ratio to 50
```

## "Attached" Property
* Primitive Type: Bool
* Keywords: ```attached, connected```
* Inverse Keywords: ```detached, disconnected```

Get/Set true wether there is a wheel attached to the suspension.
Attaching a wheel that is not placed right might invoke the almighty Clang.

```
if "My Wheel" is detached
  Print "Something stole my Wheel!"

detach "My Wheel"
attach "My Wheel"
```