import React, {useState,useEffect} from 'react';
import { useRouteMatch } from 'react-router-dom';
import { Header, ListItemLink } from 'components';
import { Items } from 'state';
import { ghHostApiUrl } from 'settings';

export interface MasterProps extends Items {
}
interface IJob {
    jobId: string,
    requestId: number,
    timeLineId: string,
    name: string,
    repo: string
    workflowname: string
    runid : number
}
interface IJobEvent {
    repo: string,
    job: IJob 
}

export const MasterContainer: React.FC<MasterProps> = (props) => {
    let { url } = useRouteMatch();
    const [ jobs, setJobs ] = useState<IJob[]>([]);
    const owner = "runner";
    const repo = "runner";
    useEffect(() => {
        var source = new EventSource(ghHostApiUrl + "/" + owner + "/" + repo + "/_apis/v1/Message/event?filter=**");
        source.addEventListener("job", ev => {
            var je = JSON.parse((ev as MessageEvent).data) as IJobEvent;
            var x = je.job;
            setJobs(jobs => {
                var insertp = jobs.findIndex(j => j.requestId < x.requestId)
                var r = jobs.filter((j) => j.requestId < x.requestId);
                r.unshift(x);
                var sp = jobs.splice(insertp);
                return [...jobs, x, ...sp];
            });
        });
        var apiUrl = ghHostApiUrl + "/" + owner + "/" + repo + "/_apis/v1/Message";
        (async () => {
            var newjobs = JSON.parse((await (await fetch(apiUrl, { })).text())) as IJob[];
            var sjobs = newjobs.sort((a, b) => b.requestId - a.requestId);
            setJobs(jobs => {
                return sjobs;
            });
        })();
    }, [])
    return (
        <React.Fragment>
            <Header title="Jobs" hideBackButton={true}/>
            <ul>
                {jobs.map((x: IJob) =>
                    <li key={x.jobId}>
                        <ListItemLink 
                            to={`${url}/detail/${encodeURIComponent(x.jobId)}`} item={{ id:  x.requestId, title: x.name, description: x.workflowname + " - " + x.repo + " - " + x.requestId }} />
                    </li>
                )}
            </ul>
        </React.Fragment>
    );
};

