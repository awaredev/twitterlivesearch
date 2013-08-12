function StartPusher(key,channel) {
    var pusher = new Pusher(key);
    pusher.connection.bind('connected', function () {
        FeedResumed();
        //alert('Real time is go!');
    });
    channel = pusher.subscribe(channel);
    channel.bind('tweets-event', function (data) {
        $('div#twitter').prepend(data.tweets);
    });
}

function FeedPaused() {
    $("#pausecontrol").hide();
    $("#resumecontrol").show();
}

function FeedResumed() {
    $("#pausecontrol").show();
    $("#resumecontrol").hide();
}