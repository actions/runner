import React, {useState,useEffect} from 'react';
import { useRouteMatch } from 'react-router-dom';
import { Header, ListItemLink } from 'components';
import { Items } from 'state';
import { ghHostApiUrl } from 'settings';
import { useParams } from 'react-router-dom';

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
    const { owner, repo } = useParams();
    useEffect(() => {
        var source = new EventSource(ghHostApiUrl + "/" + owner + "/" + repo + "/_apis/v1/Message/event?filter=**");
        source.addEventListener("job", ev => {
            var je = JSON.parse((ev as MessageEvent).data) as IJobEvent;
            var x = je.job;
            setJobs(jobs => {
                var r = jobs.filter((j) => j.requestId < x.requestId);
                r.unshift(x);
                return r;
            });
        });
        var apiUrl = ghHostApiUrl + "/" + owner + "/" + repo + "/_apis/v1/Message";
        (async () => {
            var newjobs = JSON.parse((await (await fetch(apiUrl, { })).text())) as IJob[];
            var sjobs = newjobs.sort((a, b) => b.requestId - a.requestId);
            setJobs(jobs => {
                if(jobs.length > 0) {
                    var x = jobs[jobs.length - 1];
                    sjobs = sjobs.filter((j) => j.requestId < x.requestId);
                }
                return [...sjobs, ...jobs];
            });
        })();
    }, [owner, repo])
    return (
        <React.Fragment>
            <Header title="Jobs" hideBackButton={true}/>
            <ul>
                {jobs.map((x: IJob) =>
                    <li key={x.requestId}>
                        <ListItemLink 
                            to={`${url}/detail/${x.requestId}`} item={{ id:  x.requestId, title: x.name, description: x.workflowname + " - " + x.repo + " - " + x.requestId }} />
                    </li>
                )}
            </ul>
        </React.Fragment>
    );
};

