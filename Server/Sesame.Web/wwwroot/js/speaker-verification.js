

var enrollindex = 0;
$(document).ready(function () {


    loadverificationphrases();

    createverificationprofile();

    loadenrollmenthistory();

});


function actionsAfterStopListening(blob) {
    doneEncoding(blob);
    var id = $.cookie('verificationProfileId');
    createverificationprofileenrollment(id, blob);
}
$("#verificationphrases_selector").change(function () {
    $('#verificationphrase').text($(this).val());
});


function assignpin() {



    $.ajax({
        url: VerificationSettings.assign,
        method: 'GET',
        dataType: 'json',

        success: function (data) {
            if (data) {
                console.log(data);
                var pin = data;
                //show pin
                $('.pin-info').removeClass("hidden");
                $('#pin').text(pin);
            }
        }
    });
}


function loadenrollmenthistory() {


    $.ajax({
        url: VerificationSettings.profile,
        method: 'GET',
        dataType: 'json',

        success: function (data) {
            if (data) {
                updateenrollmentinfo(data);
            }
        }
    });

}


function loadverificationphrases() {


    $.ajax({
        url: VerificationSettings.loadverificationphrases + '?locale=' + VerificationSettings.locale,
        method: 'GET',
        dataType: 'json',
        error(xhr, status, error) {

            console.log("error");

        },
        success: function (data) {
            if (data) {
                console.log(data);
                $.each(data, function (index, item) {
                    if (item) {
                        console.log(item);
                        $("#verificationphrases_selector").append('<option value="' + item + '">' + item + '</option>');
                    }

                });
            }
        }
    });
}

function createverificationprofile() {


    $.ajax({
        url: VerificationSettings.createverificationprofile,
        method: 'POST',
        //contentType: 'application/json',
        //dataType: 'json',
        data: {
            "locale": VerificationSettings.locale
        },
        error(xhr, status, error) {

            console.log("error");

        },
        success: function (data) {

            if (data) {
                console.log(data);
                var verificationProfileId = data;
                $.cookie('verificationProfileId', verificationProfileId, { expires: 7, path: '/' });

            }
        }
    });
}

function updateenrollmentinfo(data) {

    $('#enrollmessage').hide();

    $('#verificationphrase').text(data.phrase);

    var verificationCircles = $('div.verification-circle');
    for (i = 0; i < data.enrollmentsCount; i++) {
        $(verificationCircles[i]).addClass('verification-circle-success');
        $(verificationCircles[i]).removeClass('verification-circle-error');

    }

    var remainingEnrollments = data.remainingEnrollments;
    if (remainingEnrollments === undefined) {
        remainingEnrollments = data.remainingEnrollmentsCount;
    }



    $('#remainingEnrollments').text(remainingEnrollments);
    $('#enrollmentsCount').text(data.enrollmentsCount);

    if (remainingEnrollments <= 0) {

        //call api to get pin
        assignpin();

    }


}
function createverificationprofileenrollment(verificationProfileId, blob, index = 1) {
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

        var freader = new FileReader();
        freader.onload = function (e) {
            console.log(e.target.result);
            var body = e.target.result;
            var enrollurl = VerificationSettings.enroll.replace("{verificationProfileId}", verificationProfileId);
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
                    console.log(JSON.parse(xhr.responseText));

                    if (xhr.responseText && JSON.parse(xhr.responseText) && JSON.parse(xhr.responseText).error && JSON.parse(xhr.responseText).error.message) {
                        $('#enrollmessage').show().text(JSON.parse(xhr.responseText).error.message)
                    }

                    var verificationCircles = $('div.verification-circle');
                    $(verificationCircles[enrollindex]).addClass('verification-circle-error');


                },
                success: function (data) {
                    console.log(data);

                    updateenrollmentinfo(data);

                    var remainingEnrollments = data.remainingEnrollments;
                    if (!remainingEnrollments) {
                        remainingEnrollments = data.remainingEnrollmentsCount;
                    }

                    if (remainingEnrollments > 0) {






                        enrollindex = enrollindex + 1;
                    }

                }
            });
        };
        freader.readAsArrayBuffer(blob);

    }
    //start the reading process.
    reader.readAsDataURL(blob);

}


