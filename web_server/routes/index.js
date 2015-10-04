
/*
 * GET home page.
 */

var twilio = require('twilio');

var client = new twilio.RestClient('AC0662afad9cf5f3753a00b71e5b0ad975', '73deaec2c6121bea06685b15c6580fce');

exports.index = function(req, res){
	res.render('index', { title: 'Express' });
	console.log(req.body);
	sendTwilioMessage('+1 514-574-8677','+1 647-931-1254',JSON.stringify(req.body));
};


exports.wakeup = function(req, res){
	sendTwilioMessage('+1 514-574-8677','+1 647-931-1254', 'Wake up!', function(error, message) {
		res.send(200);
	});
};

exports.takeabreak = function(req, res){
	sendTwilioMessage('+1 514-574-8677','+1 647-931-1254', 'Take a break!', function(error, message) {
		res.send(200);
	});
};

function sendTwilioMessage(destinationNumber, fromNumber, body, callback) {
	client.sms.messages.create({
	    to: destinationNumber,
	    from: fromNumber,
	    body: body
	}, function(error, message) {
	    // The HTTP request to Twilio will run asynchronously. This callback
	    // function will be called when a response is received from Twilio
	    // The "error" variable will contain error information, if any.
	    // If the request was successful, this value will be "falsy"
	    if (!error) {
	        // The second argument to the callback will contain the information
	        // sent back by Twilio for the request. In this case, it is the
	        // information about the text messsage you just sent:
	        console.log('Success! The SID for this SMS message is:');
	        console.log(message.sid);
	 
	        console.log('Message sent on:');
	        console.log(message.dateCreated);
	    } else {
	        console.log('Oops! There was an error.'+JSON.stringify(body));
	        console.log('error: ' + JSON.stringify(error));
	    }
		if (callback) {
			callback(error, message);
		}
	});
}
