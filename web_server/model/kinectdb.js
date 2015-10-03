 var mongoose = require('mongoose');

 var schema = mongoose.Schema({
	created: {type: Date, default: Date.now},
	value: String,
});

/*
var Entry = mongoose.model('Entry', schema);

var newEntry = new Entry({value: "Test"});
newEntry.save(function(err){
	if (err)
		console.log("Error saving Entry to DB: " + err);
});
*/
module.exports = mongoose.model('Entry', schema);
module.exports.Entry = mongoose.model('Entry', schema);