import React, {useState,useEffect} from 'react';
import { useRouteMatch } from 'react-router-dom';
import { Header, ListItemLink } from 'components';
import { Items, Item } from 'state';
import { ghHostApiUrl } from 'settings';
import { useParams } from 'react-router-dom';

export interface MasterProps extends Items {
}
interface IJob {
    JobId: string,
    RequestId: number,
    TimeLineId: string
}
interface IJobEvent {
    repo: string,
    job: IJob 
}

export const MasterContainer: React.FC<MasterProps> = (props) => {
    let { url } = useRouteMatch();
    const [ jobs, setJobs ] = useState<Items | null>({items: []});
    const { owner, repo } = useParams();
    useEffect(() => {
        var apiUrl = ghHostApiUrl + "/" + owner + "/" + repo + "/_apis/v1/Message";
        (async () => {
            setJobs({ items: (JSON.parse((await (await fetch(apiUrl, { })).text())) as IJob[]).sort((a, b) => b.RequestId - a.RequestId).map((x : IJob) : Item => { return { id:  x.RequestId, title: x.JobId, description: x.TimeLineId }})});
        })();
        var source = new EventSource(ghHostApiUrl + "/" + owner + "/" + repo + "/_apis/v1/Message/event");
        source.addEventListener("job", ev => {
            var je = JSON.parse((ev as MessageEvent).data) as IJobEvent;
            var x = je.job;
            setJobs((jobs) => {
                return { items: [{ id:  x.RequestId, title: x.JobId, description: x.TimeLineId }, ...jobs.items] };
            });
        });
    }, [owner, repo])
    return (
        <React.Fragment>
            <Header title="Jobs" hideBackButton={true}/>
            <ul>
                {jobs.items.map((item: Item) =>
                    <li key={item.id}>
                        <ListItemLink 
                            to={`${url}/detail/${item.id}`} item={item} />
                    </li>
                )}
            </ul>
        </React.Fragment>
    );
};

