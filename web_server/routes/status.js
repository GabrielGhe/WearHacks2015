
/*
 * GET users listing.
 */
 
var kinectdb = require("../model/kinectdb");

exports.get = function(req, res){
	kinectdb.find({}).sort({created: -1}).exec(function(err, entry) {
		if (err)
			console.log("Error getting Entry from DB: " + err);
		if (entry)
			if(entry[0])
				res.json(entry[0]);
			else
				res.json(entry);
		else
			res.json({Error: 'No Entry found in database.'});
	});
};

exports.post = function(req, res){
	var newEntry = new kinectdb.Entry({value: JSON.stringify(req.body)});
	newEntry.save(function(err){
		if (err) {
			console.log("Error saving Entry to DB: " + err);
			res.send(404);
		}
		res.send(200);
	});
};