function StartPusher(key,channel) {
    var pusher = new Pusher(key);
    pusher.connection.bind('connected', function () {
        $("#pausecontrol").show();
        $("#resumecontrol").hide();
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
    $("#waiting").text("");
}

function FeedResumed() {
    $("#pausecontrol").show();
    $("#resumecontrol").hide();
    $("#waiting").text("");
}

function AwaitPause() {
    $("#pausecontrol").hide();
    $("#resumecontrol").hide();
    $("#waiting").text("Pausing Feed....");
}

function AwaitResume() {
    $("#pausecontrol").hide();
    $("#resumecontrol").hide();
    $("#waiting").text("Resuming Feed....");
}