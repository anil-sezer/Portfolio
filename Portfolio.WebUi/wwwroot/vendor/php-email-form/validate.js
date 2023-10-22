(function () {
    "use strict";

    let forms = document.querySelectorAll('.php-email-form');

    forms.forEach(function (e) {
        e.addEventListener('submit', function (event) {
            event.preventDefault();
            let thisForm = this;

            thisForm.querySelector('.loading').classList.add('d-block');
            thisForm.querySelector('.error-message').classList.remove('d-block');
            thisForm.querySelector('.sent-message').classList.remove('d-block');

            let formData = new FormData(thisForm);

            // Use Fetch API to submit the form data
            fetch('/', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': getAntiForgeryToken(thisForm)
                },
                body: formData
            })
                .then(response => response.json())
                .then(data => {
                    thisForm.querySelector('.loading').classList.remove('d-block');
                    thisForm.querySelector('.sent-message').classList.add('d-block');
                    thisForm.reset();
                })
                .catch((error) => {
                    thisForm.querySelector('.loading').classList.remove('d-block');
                    thisForm.querySelector('.error-message').innerHTML = "An error occurred";
                    thisForm.querySelector('.error-message').classList.add('d-block');
                });
        });
    });

    function getAntiForgeryToken(form) {
        return form.querySelector('input[name="__RequestVerificationToken"]').value;
    }

})();



// THIS WORKS BUT REFRESHES THE PAGE
// (function () {
//   "use strict";
//
//   let forms = document.querySelectorAll('.php-email-form');
//
//   forms.forEach(function (e) {
//     e.addEventListener('submit', function (event) {
//       let thisForm = this;
//
//       thisForm.querySelector('.loading').classList.add('d-block');
//       thisForm.querySelector('.error-message').classList.remove('d-block');
//       thisForm.querySelector('.sent-message').classList.remove('d-block');
//
//       // Assuming the form is valid, remove the loading class
//       thisForm.querySelector('.loading').classList.remove('d-block');
//
//       // Add logic here to determine if the email was sent successfully.
//       // For now, I'm adding the sent-message class unconditionally.
//       thisForm.querySelector('.sent-message').classList.add('d-block');
//
//       thisForm.reset();
//     });
//   });
//
// })();


//
// /**
// * PHP Email Form Validation - v3.6
// * URL: https://bootstrapmade.com/php-email-form/
// * Author: BootstrapMade.com
// */
// (function () {
//   "use strict";
//
//   let forms = document.querySelectorAll('.php-email-form');
//
//   forms.forEach( function(e) {
//     e.addEventListener('submit', function(event) {
//       event.preventDefault();
//
//       let thisForm = this;
//
//       let action = thisForm.getAttribute('action');
//
//       // if( ! action ) {
//       //   displayError(thisForm, 'The form action property is not set!');
//       //   return;
//       // }
//       thisForm.querySelector('.loading').classList.add('d-block');
//       thisForm.querySelector('.error-message').classList.remove('d-block');
//       thisForm.querySelector('.sent-message').classList.remove('d-block');
//
//       let formData = new FormData( thisForm );
//
//         php_email_form_submit(thisForm, action, formData);
//     });
//   });
//
//   function php_email_form_submit(thisForm, action, formData) {
//     fetch(action, {
//       method: 'POST',
//       body: formData,
//       headers: {'X-Requested-With': 'XMLHttpRequest'}
//     })
//     .then(response => {
//       if( response.ok ) {
//         return response.text();
//       } else {
//         throw new Error(`${response.status} ${response.statusText} ${response.url}`); 
//       }
//     })
//     .then(data => {
//       thisForm.querySelector('.loading').classList.remove('d-block');
//       if (data.trim() == 'OK') {
//         thisForm.querySelector('.sent-message').classList.add('d-block');
//         thisForm.reset(); 
//       } else {
//         throw new Error(data ? data : 'Form submission failed and no error message returned from: ' + action); 
//       }
//     })
//     .catch((error) => {
//       displayError(thisForm, error);
//     });
//   }
//
//   function displayError(thisForm, error) {
//     thisForm.querySelector('.loading').classList.remove('d-block');
//     thisForm.querySelector('.error-message').innerHTML = error;
//     thisForm.querySelector('.error-message').classList.add('d-block');
//   }
// })();
