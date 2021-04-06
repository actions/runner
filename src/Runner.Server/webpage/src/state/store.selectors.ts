import { Item } from './example.model';

export interface IJob {
    JobId: string,
    RequestId: number,
    TimeLineId: string
    name: string,
    repo: string
    workflowname: string
    runid : number
    errors: string[]
}

export const getJobById = (jobs : IJob[], id: number | string | undefined): { item: Item | null, job: IJob | null } => {
    const actualId = (typeof id === 'string') ?
        parseInt(id, 10): id;
    var item : Item | null = null;
    if(actualId !== undefined && actualId !== null) {
        var x : IJob | null = jobs.find((x, i, obj) => x.RequestId === actualId) || null;
        if(x !== null) {
            item = { id:  x.RequestId, title: x.JobId, description: x.TimeLineId }
            return { item : item, job:x };
        }
    }
    return { item : null, job : null };
};
