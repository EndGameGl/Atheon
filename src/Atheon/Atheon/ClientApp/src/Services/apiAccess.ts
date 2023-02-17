import ApiResponse from "../Models/ApiResponse";

async function callApi<T>(
    url: string,
    body: any,
    method: string): Promise<ApiResponse<T>> {
    const response = await fetch(
        url,
        {
            method: method,
            body: body ? JSON.stringify(body) : null,
            headers: {
                'Content-Type': 'application/json'
            }
        });
    const result = await response.json() as ApiResponse<T>;
    return result;
}

export default callApi;