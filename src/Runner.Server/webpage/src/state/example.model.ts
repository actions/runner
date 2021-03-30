export class ExampleState {
    constructor(
        public items: { [id:number]: Item },
        public ids: number[] = []
    ) {}
}

export class Items {
    public items: Item[] = [];
}

export interface Item {
    id: number;
    title: string;
    description: string;
}