import React from 'react'; 
import { Item } from 'state';
import styles from './ListItem.module.scss';

export interface ListItemProps {
    item: Item
};

export const listItemNoDataMessage = 'No Data';

export const ListItem: React.FC<ListItemProps> = (props) => {
    
    return (
        <div className={styles.component}>

            <div className={styles.inner}>

                <h1 data-test="ListItemHeading">
                    { props.item.title ? props.item.title : listItemNoDataMessage  }
                </h1> 

                <h2 data-test="ListItemSubHeading">
                    { props.item.description ?  props.item.description : listItemNoDataMessage }
                </h2>

            </div>

        </div> 
    );
};

export default ListItem;