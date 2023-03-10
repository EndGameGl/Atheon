interface ApiResponse<T> {
    Data: T | null;
    Message: string | null;
    Code: number;
}

export default ApiResponse;