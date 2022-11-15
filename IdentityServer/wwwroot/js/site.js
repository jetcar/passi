// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

var redirectActive = true;
function stopRedirect() {
    redirectActive = false;
}

function refreshCheckForm() {
    setTimeout(function () {

        $("#refreshBtn").each(function () {
            if (redirectActive)
                $(this).click();
        });


    }, 5000);
}

refreshCheckForm();