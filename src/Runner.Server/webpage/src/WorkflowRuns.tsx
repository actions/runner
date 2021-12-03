import React, {useState,useEffect} from 'react';
import { useRouteMatch, Link, useLocation, useParams } from 'react-router-dom';
import { Header, ListItemLink } from 'components';
import { Items } from 'state';
import { ghHostApiUrl } from 'settings';

export interface WorkflowRunsProps {
}
interface IWorkflowRun {
    id: string,
    fileName: string
}

export const WorkflowRuns: React.FC<WorkflowRunsProps> = (props) => {
    let { url } = useRouteMatch();
    const [ owners, setOwners ] = useState<IWorkflowRun[]>([]);
    const { page, owner, repo } = useParams<{page: string, owner: string, repo: string}>();
    useEffect(() => {
        var apiUrl = `${ghHostApiUrl}/_apis/v1/Message/workflow/runs?owner=${encodeURIComponent(owner)}&repo=${encodeURIComponent(repo)}&page=${page || "0"}`;
        (async () => {
            var newOwners = JSON.parse((await (await fetch(apiUrl, { })).text())) as IWorkflowRun[];
            setOwners(_ => {
                return newOwners;
            });
        })();
    }, [page, owner, repo])
    const location = useLocation<string>();
    return (
        <React.Fragment>
            <Header title={`WorkflowRuns ${owner}/${repo}`} hideBackButton={false} content={
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
                {owners.map((x: IWorkflowRun) =>
                    <li key={x.id}>
                        <ListItemLink 
                            to={`${url}/${encodeURIComponent(x.id)}`} item={{ id:  x.id, title: x.id, description: x.fileName }} />
                    </li>
                )}
            </ul>
        </React.Fragment>
    );
};

