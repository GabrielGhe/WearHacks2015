//Author: Tim Smith
////////////////

//vars and counters
var eyesClosedCtr = 0; // how long eyes have been closed
var sleepThreshold = 300; //amount of time til eyes clsoed is considered sleeping

var sleepCtr = 0; //how long user is asleep
var napThreshold = 3000; // how long a sleep is allowed to last

var restCtr = 0; //how long a rest/walk has been
var restThreshold = 500; //how long a rest/walk is allowed to endure before a pebble notification

var workCtr = 0; //how long a user has been working
var workThreshold = 2000; //how long a work session goes until rest is called for

UserState = {
	SLEEP : 1,
	WORK : 2,
	REST : 3
}

/////
// looping function
/////
update() {
	userState = getAwakeState();

	if(userState == UserState.SLEEP) {
		sleep();
	}
	else {
		if(userState == UserState.WORK) {
			work();
		}
		else {
			rest();
		}
	}

}

getUserState() {

	//TODO: Handle the 'noPerson data'
	if(noPerson) { //kinect sees noone
		eyesClosedCtr = 0;
		return UserState.REST;
	}
	
	//TODO: Handle the 'eyesClosed data'
	if (eyesClosed) {
		eyesClosedCtr++;
	}
	else {
		eyesClosedCtr = 0;
		return UserState.WORK;
	}

	if(eyesClosedCtr > sleepThreshold){
		return UserState.SLEEP;
	}

}

sleep() {
	workCtr = 0;
	restCtr = 0;

	sleepCtr++;

	if(sleepCtr > napThreshold) {
		Pebble.sendWakeup();
	}
}

work() {
	sleepCtr = 0;
	restCtr = 0;

	workCtr++;

	if(workCtr > workThreshold) {
		Pebble.sendBreak();
	}
}

rest() {
	sleepCtr = 0;
	workCtr = 0;

	restCtr++;

	if(restCtr > restThreshold) {
		Pebble.sendWork();
	}
}

while(true) {
	update();
	//exit conditions are silly
	break;
}