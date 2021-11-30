import React, {useState,useEffect} from 'react';
import { useRouteMatch, Link, useLocation, useParams } from 'react-router-dom';
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
    attempt : number
}
interface IJobEvent {
    repo: string,
    job: IJob 
}

export const MasterContainer: React.FC<MasterProps> = (props) => {
    let { url } = useRouteMatch();
    const [ jobs, setJobs ] = useState<IJob[]>([]);
    const { page } = useParams<{page: string}>();
    const owner = "runner";
    const repo = "runner";
    useEffect(() => {
        var source = new EventSource(ghHostApiUrl + "/" + owner + "/" + repo + "/_apis/v1/Message/event?filter=**");
        try {
            if(!page || page === "0") {
                source.addEventListener("job", ev => {
                    var je = JSON.parse((ev as MessageEvent).data) as IJobEvent;
                    var x = je.job;
                    setJobs(jobs => {
                        var insertp = jobs.findIndex(j => j.runid < x.runid || j.attempt < x.attempt || j.requestId < x.requestId)
                        var sp = jobs.splice(insertp);
                        var final = [...jobs, x, ...sp];
                        // Remove elements from first page
                        if(final.length > 30) {
                            final.length = 30;
                        }
                        return final;
                    });
                });
            }
            var apiUrl = ghHostApiUrl + "/" + owner + "/" + repo + "/_apis/v1/Message?page=" + (page || "0");
            (async () => {
                var newjobs = JSON.parse((await (await fetch(apiUrl, { })).text())) as IJob[];
                var sjobs = newjobs.sort((j, x) => x.runid - j.runid || x.attempt - j.attempt || x.requestId - j.requestId || 0);
                setJobs(_ => {
                    return sjobs;
                });
            })();
        } finally {
            return () => {
                source.close();
            }
        }
    }, [page])
    const location = useLocation<string>();
    return (
        <React.Fragment>
            <Header title="Jobs" hideBackButton={true} content={
                <div>
                    <Link to={location.pathname.replace("/" + (page || "master"), "/" + (Number.parseInt(page || "0") - 1))} style={{ visibility: location.pathname.startsWith('/master/') || location.pathname === '/master' || location.pathname.startsWith('/0/') || location.pathname === '/0' ? 'hidden' : 'visible' }}>
                        Previous
                    </Link>
                    <Link to={location.pathname.replace("/" + (page || "master"), "/" + (Number.parseInt(page || "0") + 1))} >
                        Next
                    </Link>
                </div>
            }/>
            <ul>
                {jobs.map((x: IJob) =>
                    <li key={x.jobId}>
                        <ListItemLink 
                            to={`${url}/detail/${encodeURIComponent(x.jobId)}`} item={{ id:  x.jobId, title: x.name, description: x.workflowname + " " + x.repo + " " + x.runid }} />
                    </li>
                )}
            </ul>
        </React.Fragment>
    );
};

