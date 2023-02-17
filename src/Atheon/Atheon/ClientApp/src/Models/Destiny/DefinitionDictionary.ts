export interface DefinitionDictionary<T> {
    Type: string;
    [Hash: number]: T
}