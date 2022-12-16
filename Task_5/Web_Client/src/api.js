/* eslint-disable */
/*
    TsirleoService generated
    Service for image processing and storage
    version: v1
*/

export function Configuration(config) {
    this.basePath = 'https://localhost:7173';
    this.fetchMethod = window.fetch;
    this.headers = {};
    this.getHeaders = () => { return {} };
    this.responseHandler = null;
    this.errorHandler = null;

    if (config) {
        if (config.basePath) {
            this.basePath = config.basePath;
        }
        if (config.fetchMethod) {
            this.fetchMethod = config.fetchMethod;
        }
        if (config.headers) {
            this.headers = config.headers;
        }
        if (config.getHeaders) {
            this.getHeaders = config.getHeaders;
        }
        if (config.responseHandler) {
            this.responseHandler = config.responseHandler;
        }
        if (config.errorHandler) {
            this.errorHandler = config.errorHandler;
        }
    }
}

const setAdditionalParams = (params = [], additionalParams = {}) => {
    Object.keys(additionalParams).forEach(key => {
        if (additionalParams[key]) {
            params.append(key, additionalParams[key]);
        }
    });
};

export function ServerControllersApi(config) {
    this.config = config || new Configuration();

    this.imagesPost = (args, options = {}) => {
        const { data } = args;
        const { fetchMethod, basePath, headers, getHeaders, responseHandler, errorHandler } = this.config;
        let url = '/images';
        const params = new URLSearchParams();
        setAdditionalParams(params, options.params);
        const query = params.toString();
        const controller = new AbortController();
        const promise = new Promise((resolve, reject) => {
            const promise = fetchMethod(basePath + url + (query ? '?' + query : ''), {
                signal: controller.signal,
                method: 'post',
                headers: { 'Content-Type': 'application/json', ...headers, ...getHeaders(), ...options.headers },
                body: 'object' === typeof data ? JSON.stringify(data) : data
            });
            !!errorHandler && promise.catch(errorHandler);
            const catcher = error => error?.name !== "AbortError" && reject(error);
            !!responseHandler && promise.then(responseHandler, catcher);
            if (options.returnResponse) {
                promise.then(response => resolve(response), catcher);
            } else {
                promise.then(response => {
                    if (response.status === 200 || response.status === 204 || response.status === 304) {
                        return response.json();
                    } else {
                        reject(response);
                    }
                }, catcher).then(data => resolve(data), catcher);
            }
        });
        promise.abort = () => controller.abort();
        return promise;
    };

    this.imagesGet = (options = {}) => {
        const { fetchMethod, basePath, headers, getHeaders, responseHandler, errorHandler } = this.config;
        let url = '/images';
        const params = new URLSearchParams();
        setAdditionalParams(params, options.params);
        const query = params.toString();
        const controller = new AbortController();
        const promise = new Promise((resolve, reject) => {
            const promise = fetchMethod(basePath + url + (query ? '?' + query : ''), {
                signal: controller.signal,
                method: 'get',
                headers: { ...headers, ...getHeaders(), ...options.headers }
            });
            !!errorHandler && promise.catch(errorHandler);
            const catcher = error => error?.name !== "AbortError" && reject(error);
            !!responseHandler && promise.then(responseHandler, catcher);
            if (options.returnResponse) {
                promise.then(response => resolve(response), catcher);
            } else {
                promise.then(response => {
                    if (response.status === 200 || response.status === 204 || response.status === 304) {
                        return response.json();
                    } else {
                        reject(response);
                    }
                }, catcher).then(data => resolve(data), catcher);
            }
        });
        promise.abort = () => controller.abort();
        return promise;
    };

    this.imagesDelete = (options = {}) => {
        const { fetchMethod, basePath, headers, getHeaders, responseHandler, errorHandler } = this.config;
        let url = '/images';
        const params = new URLSearchParams();
        setAdditionalParams(params, options.params);
        const query = params.toString();
        const controller = new AbortController();
        const promise = new Promise((resolve, reject) => {
            const promise = fetchMethod(basePath + url + (query ? '?' + query : ''), {
                signal: controller.signal,
                method: 'delete',
                headers: { ...headers, ...getHeaders(), ...options.headers }
            });
            !!errorHandler && promise.catch(errorHandler);
            const catcher = error => error?.name !== "AbortError" && reject(error);
            !!responseHandler && promise.then(responseHandler, catcher);
            promise.then(response => {
                if (response.status === 200 || response.status === 204 || response.status === 304) {
                    resolve(response);
                } else {
                    reject(response);
                }
            }, catcher);
        });
        promise.abort = () => controller.abort();
        return promise;
    };

    this.imagesIdGet = (args, options = {}) => {
        const { id } = args;
        const { fetchMethod, basePath, headers, getHeaders, responseHandler, errorHandler } = this.config;
        let url = '/images/{id}';
        url = url.split(['{', '}'].join('id')).join(encodeURIComponent(String(id)));
        const params = new URLSearchParams();
        setAdditionalParams(params, options.params);
        const query = params.toString();
        const controller = new AbortController();
        const promise = new Promise((resolve, reject) => {
            const promise = fetchMethod(basePath + url + (query ? '?' + query : ''), {
                signal: controller.signal,
                method: 'get',
                headers: { ...headers, ...getHeaders(), ...options.headers }
            });
            !!errorHandler && promise.catch(errorHandler);
            const catcher = error => error?.name !== "AbortError" && reject(error);
            !!responseHandler && promise.then(responseHandler, catcher);
            if (options.returnResponse) {
                promise.then(response => resolve(response), catcher);
            } else {
                promise.then(response => {
                    if (response.status === 200 || response.status === 204 || response.status === 304) {
                        return response.json();
                    } else {
                        reject(response);
                    }
                }, catcher).then(data => resolve(data), catcher);
            }
        });
        promise.abort = () => controller.abort();
        return promise;
    };

    this.imagesIdDelete = (args, options = {}) => {
        const { id } = args;
        const { fetchMethod, basePath, headers, getHeaders, responseHandler, errorHandler } = this.config;
        let url = '/images/{id}';
        url = url.split(['{', '}'].join('id')).join(encodeURIComponent(String(id)));
        const params = new URLSearchParams();
        setAdditionalParams(params, options.params);
        const query = params.toString();
        const controller = new AbortController();
        const promise = new Promise((resolve, reject) => {
            const promise = fetchMethod(basePath + url + (query ? '?' + query : ''), {
                signal: controller.signal,
                method: 'delete',
                headers: { ...headers, ...getHeaders(), ...options.headers }
            });
            !!errorHandler && promise.catch(errorHandler);
            const catcher = error => error?.name !== "AbortError" && reject(error);
            !!responseHandler && promise.then(responseHandler, catcher);
            promise.then(response => {
                if (response.status === 200 || response.status === 204 || response.status === 304) {
                    resolve(response);
                } else {
                    reject(response);
                }
            }, catcher);
        });
        promise.abort = () => controller.abort();
        return promise;
    };

    this.imagesEmotionEmotionGet = (args, options = {}) => {
        const { emotion } = args;
        const { fetchMethod, basePath, headers, getHeaders, responseHandler, errorHandler } = this.config;
        let url = '/images/emotion/{emotion}';
        url = url.split(['{', '}'].join('emotion')).join(encodeURIComponent(String(emotion)));
        const params = new URLSearchParams();
        setAdditionalParams(params, options.params);
        const query = params.toString();
        const controller = new AbortController();
        const promise = new Promise((resolve, reject) => {
            const promise = fetchMethod(basePath + url + (query ? '?' + query : ''), {
                signal: controller.signal,
                method: 'get',
                headers: { ...headers, ...getHeaders(), ...options.headers }
            });
            !!errorHandler && promise.catch(errorHandler);
            const catcher = error => error?.name !== "AbortError" && reject(error);
            !!responseHandler && promise.then(responseHandler, catcher);
            if (options.returnResponse) {
                promise.then(response => resolve(response), catcher);
            } else {
                promise.then(response => {
                    if (response.status === 200 || response.status === 204 || response.status === 304) {
                        return response.json();
                    } else {
                        reject(response);
                    }
                }, catcher).then(data => resolve(data), catcher);
            }
        });
        promise.abort = () => controller.abort();
        return promise;
    };
}


