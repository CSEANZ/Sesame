
//var activitytimer;
var recordertimer;
var welcomeScreen = $("#welcome-screen");
var profileScreen = $("#profile-screen");
var verifyScreen = $("#verify-screen");

var currentScreen;

$(document).ready(function () {

    var pressedKey = {};



    function startOver() {

        currentScreen = welcomeScreen;
        hideElement(profileScreen);
        hideElement(verifyScreen);
        $('#message').removeClass();


    }

    startOver();

    window.addEventListener('keydown', function (e) {


        if (currentScreen == welcomeScreen) {
            hideElement(welcomeScreen);
            showElement(profileScreen);
            currentScreen = profileScreen;
            $('#pin').focus();



            var pinrecognition = new webkitSpeechRecognition();
            pinrecognition.onstart = function (event) {
                $('#pinload').hide();
            }
            pinrecognition.onresult = function (event) {

                var trans = event.results[0][0].transcript;
                trans = trans.replace(/[^0-9.]/g, "");

                if (trans != "") {

                    $('#pin').val(trans);
                    pinrecognition.stop();

                    var pinwait = setTimeout(function () {
                        $('#pinsubmit').submit();
                    }, 1000);
                }
            }
            pinrecognition.start();
        }


        pressedKey[e.keyCode || e.which] = true;
    }, true);




    $(".login-group").on("submit", function (event) {



        event.preventDefault();

        var pin = $('#pin').val();
        //alert(pin);
        getphrase(pin);

    });

});

function actionsAfterStopListening(blob) {

    clearTimeout(recordertimer);
    doneEncoding(blob);

    //call api
    var pin = $('#pin').val();
    console.log('calling actionsAfterStopListening');
    verifyprofile(pin, blob);
}


function hideElement(domElement) {
    $(domElement).addClass('hidden');
};

function showElement(domElement) {
    $(domElement).removeClass('hidden');
}

function verifypin(pin) {

    return new Promise((resolve, reject) => {

        $.ajax({
            url: VerificationSettings.verifypin.replace("{pin}", pin),
            method: 'GET',
            dataType: 'json',
            error(xhr, status, error) {
                reject(xhr);
            },
            success: function (data) {
                resolve(data);

            }
        });
    });
}

function getphrase(pin) {

   // alert('getphrase');
   // alert(VerificationSettings.phrase.replace("{pin}", pin.trim()));
    $.ajax({
        url: VerificationSettings.phrase.replace("{pin}", pin.trim()),
        method: 'GET',
        dataType: 'json',
        error(xhr, status, error) {
            console.log('error');
            $('#message').show().text('pin is invalid');

        },
        success: function (data) {



            console.log(data);

            $('#message').show().text('start listening shortly ....');
            $('#verificationphrase').show().text(data.phrase);
            hideElement(profileScreen);
            showElement(verifyScreen);
            currentScreen = verifyScreen;



            $("#verify-prompt").text("Please say your verification phrase.");
            // Start listener


            recordertimer = setTimeout(function () {
                $('#message').show().text('recording!go!');
                startListening();
                // start recording
                recordertimer = setTimeout(function () {

                    $('#message').show().text('');
                    // Stop listener
                    stopListening(actionsAfterStopListening);
                    //  }
                }, 4000);

            }, 500);

        }
    });

}


function verifyprofile(pin, blob) {
    /*
  The audio file should be at least 1-second-long and no longer than 15 seconds. Each speaker must provide at least three enrollments to the service.
  The audio file format must meet the following requirements.
  Container	WAV
  Encoding	PCM
  Rate	16K
  Sample Format	16 bit
  Channels	Mono
  */

    var reader = new FileReader();
    reader.onload = function (event) {

        //file = input.files[0];
        var freader = new FileReader();
        freader.onload = function (e) {
            console.log(e.target.result);
            body = e.target.result;
            var enrollurl = VerificationSettings.verifyvoice.replace("{pin}", pin.trim());
            $.ajax({
                type: 'POST',

                url: enrollurl,
                headers: {
                    'Content-type': 'application/octet-stream'
                },
                data: body,
                cache: false,
                contentType: false,
                processData: false,
                method: 'POST',
                error(xhr, status, error) {
                    console.log("error");
                    console.log((xhr.responseText));

                    if (xhr.responseText && JSON.parse(xhr.responseText) && JSON.parse(xhr.responseText).error.message) {
                        $('#message').show().text(JSON.parse(xhr.responseText).error.message);
                    }

                },
                success: function (data) {
                    console.log(data);

                    $('#message').show().text(data.result);
                    $('#message').removeClass();
                    $('#message').addClass(data.result.toLowerCase());

                    if (data.result.toLowerCase() === 'accept') {
                        redirect();
                    }
                   

                }
            });
        };
        freader.readAsArrayBuffer(blob);

    }
    //start the reading process.
    reader.readAsDataURL(blob);

}

// Read a page's GET URL variables and return them as an associative array.
function getUrlVars() {
    var vars = [], hash;
    var hashes = window.location.href.slice(window.location.href.indexOf('?') + 1).split('&');
    for (var i = 0; i < hashes.length; i++) {
        hash = hashes[i].split('=');
        vars.push(hash[0]);
        vars[hash[0]] = hash[1];
    }
    return vars;
}

function redirect() {

    $('#authenticationform').submit();

    //var redirect_uri = $('input[name="redirect_uri"]').val();
    //var token = $('input[name="__RequestVerificationToken"]').val();
    //var requestId = $('input[name="request_id"]').val();
    //var data = {
    //    request_id: requestId,
    //    'submit.Accept': true,
    //    __RequestVerificationToken: token,
    //    client_id: 'mvc',
    //    response_type: 'code',
    //    redirect_uri: redirect_uri
    //};


    //$.ajax({
    //    url: '/connect/authorize',
    //    method: 'POST',
    //    contentType: 'application/x-www-form-urlencoded',

    //    data: data,
    //    error(xhr, status, error) {

    //        console.log("error");

    //    },
    //    success: function (data) {

    //        if (data) {
    //            console.log(data);
    //            var verificationProfileId = data;
    //            $.cookie('verificationProfileId', verificationProfileId, { expires: 7, path: '/' });

    //        }
    //    }
    //});


}

