const form = document.getElementById('email-form');
const submitBtn = document.getElementById('submit-btn');
const btnText = document.getElementById('btn-text');
const btnSpinner = document.getElementById('btn-spinner');
const floatingAlert = document.getElementById('floating-alert');

const fieldIds = ['name', 'email', 'subject', 'message'];
const minLengths = { name: 3, email: null, subject: 2, message: 15 };

form.addEventListener('submit', async (e) => {
    e.preventDefault();

    console.log('⭐ Form submitted');
    if (!validateForm()) {
        return;
    }

    submitBtn.disabled = true;
    btnText.style.display = 'none';
    btnSpinner.style.display = 'inline-block';

    const formData = {
        name: document.getElementById('name').value.trim(),
        email: document.getElementById('email').value.trim(),
        subject: document.getElementById('subject').value.trim(),
        message: document.getElementById('message').value.trim()
    };

    console.log('⭐ Form submitted formdata here:', formData);

    try {
        const response = await fetch('/api/email/send', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(formData)
        });

        const result = await response.json();
        console.log('⭐ response', result);
        

        showAlert(result.success, result.message);

        if (result.success) {
            form.reset();
            clearErrors();
        }
    } catch (error) {
        showAlert(false, 'An error occurred. Please try again later.');
    } finally {
        submitBtn.disabled = false;
        btnText.style.display = 'inline';
        btnSpinner.style.display = 'none';
    }
});

function validateForm() {
    clearErrors();
    let isValid = true;

    for (const fieldId of fieldIds) {
        const field = document.getElementById(fieldId);
        const errorEl = document.getElementById(`${fieldId}-error`);
        let error = '';

        if (!field.value.trim()) {
            error = 'This field is required';
            isValid = false;
        } else if (fieldId === 'email') {
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (!emailRegex.test(field.value)) {
                error = 'Invalid email format';
                isValid = false;
            }
        } else if (minLengths[fieldId] && field.value.trim().length < minLengths[fieldId]) {
            error = `Must be at least ${minLengths[fieldId]} characters`;
            isValid = false;
        }

        if (error) {
            field.classList.add('is-invalid');
            errorEl.textContent = error;
            errorEl.classList.add('show');
        }
    }

    return isValid;
}

function clearErrors() {
    for (const fieldId of fieldIds) {
        const field = document.getElementById(fieldId);
        const errorEl = document.getElementById(`${fieldId}-error`);
        field.classList.remove('is-invalid');
        errorEl.classList.remove('show');
        errorEl.textContent = '';
    }
}

function showAlert(success, message) {
    floatingAlert.className = success ? 'floating-alert alert-success' : 'floating-alert alert-danger';
    floatingAlert.textContent = message;
    floatingAlert.style.display = 'block';

    setTimeout(() => {
        floatingAlert.style.opacity = '0';
        setTimeout(() => {
            floatingAlert.style.display = 'none';
            floatingAlert.style.opacity = '1';
        }, 500);
    }, 5000);
}