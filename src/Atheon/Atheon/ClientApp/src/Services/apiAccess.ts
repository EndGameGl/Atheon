async function callApi<T>(
    url: string,
    body: any,
    method: string): Promise<ApiResponse<T>> {
    const response = await fetch(
        url,
        {
            method: method,
            body: JSON.stringify(body),
            headers: {
                'Content-Type': 'application/json'
            }
        });
    const result = await response.json() as ApiResponse<T>;
    return result;
}

export default callApi;