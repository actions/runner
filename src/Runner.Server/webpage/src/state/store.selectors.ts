import { Item } from './example.model';

export interface IJobCompletedEvent {
    jobId: string,
    requestId: number,
    result: string
}
export interface IJob {
    jobId: string,
    requestId: number,
    timeLineId: string,
    name: string,
    repo: string,
    workflowname: string,
    runid : number,
    errors: string[],
    result: string,
}

export const getJobById = (jobs : IJob[], id: number | string | undefined): { item: Item | null, job: IJob | null } => {
    const actualId = (typeof id === 'string') ?
        parseInt(id, 10): id;
    var item : Item | null = null;
    if(actualId !== undefined && actualId !== null) {
        var x : IJob | null = jobs.find((x, i, obj) => x.requestId === actualId) || null;
        if(x !== null) {
            item = { id:  x.requestId, title: x.jobId, description: x.timeLineId }
            return { item : item, job:x };
        }
    }
    return { item : null, job : null };
};
