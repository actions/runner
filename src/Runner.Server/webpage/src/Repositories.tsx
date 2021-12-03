import React, {useState,useEffect} from 'react';
import { useRouteMatch, Link, useLocation, useParams } from 'react-router-dom';
import { Header, ListItemLink } from 'components';
import { Items } from 'state';
import { ghHostApiUrl } from 'settings';

export interface RepositoriesProps {
}
interface IRepository {
    name: string,
}

export const Repositories: React.FC<RepositoriesProps> = (props) => {
    let { url } = useRouteMatch();
    const [ owners, setOwners ] = useState<IRepository[]>([]);
    const { page, owner } = useParams<{page: string, owner: string}>();
    useEffect(() => {
        var apiUrl = `${ghHostApiUrl}/_apis/v1/Message/repositories?owner=${encodeURIComponent(owner)}&page=${page || "0"}`;
        (async () => {
            var newOwners = JSON.parse((await (await fetch(apiUrl, { })).text())) as IRepository[];
            setOwners(_ => {
                return newOwners;
            });
        })();
    }, [page, owner])
    const location = useLocation<string>();
    return (
        <React.Fragment>
            <Header title={`Repositories ${owner}`} hideBackButton={false} content={
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
                {owners.map((x: IRepository) =>
                    <li key={x.name}>
                        <ListItemLink 
                            to={`${url}/${encodeURIComponent(x.name)}`} item={{ id:  x.name, title: x.name, description: "" }} />
                    </li>
                )}
            </ul>
        </React.Fragment>
    );
};

