/**
 * Module dependencies.
 */

var express = require('express');
var routes = require('./routes');
var status = require('./routes/status');
var http = require('http');
var path = require('path');

//Mongoose connecting
//---------------------------------------------
var mongoose = require('mongoose');
mongoose.connect('mongodb://localhost/kinectdb');//db name
mongoose.connection.on('error', function() {
  console.error('✗ MongoDB Connection Error. Please make sure MongoDB is running.');
});

var app = express();

// all environments
app.set('port', process.env.PORT || 3000);
app.set('views', path.join(__dirname, 'views'));
app.set('view engine', 'ejs');
app.use(express.favicon());
app.use(express.logger('dev'));
app.use(express.json());
app.use(express.urlencoded());
app.use(express.methodOverride());
app.use(express.cookieParser('your secret here'));
app.use(express.session());
app.use(app.router);
app.use(express.static(path.join(__dirname, 'public')));

// development only
if ('development' == app.get('env')) {
  app.use(express.errorHandler());
}

app.get('/', routes.index);
app.get('/takeabreak', routes.takeabreak);
app.get('/wakeup', routes.wakeup);
app.get('/status', status.getLatest);
app.get('/status/all', status.getAll);
app.post('/status', status.post);
app.post('/',routes.index);

http.createServer(app)
app.listen(app.get('port'), function(){
  console.log('Express server listening on port ' + app.get('port'));
});