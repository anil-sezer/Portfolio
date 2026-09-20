export const HttpMethod = Object.freeze({
    GET: 'GET',
    POST: 'POST',
    PUT: 'PUT',
    DELETE: 'DELETE'
});

export const HttpStatus = Object.freeze({
    OK: 200,
    BAD_REQUEST: 400,
    TOO_MANY_REQUESTS: 429,
    INTERNAL_SERVER_ERROR: 500
});

export const HttpHeaders = Object.freeze({
    CONTENT_TYPE: 'Content-Type',
    X_CSRF_TOKEN: 'X-CSRF-TOKEN'
});
