export class Items {
    public items: Item[] = [];
}

export interface Item {
    id: string;
    title: string;
    description: string;
}