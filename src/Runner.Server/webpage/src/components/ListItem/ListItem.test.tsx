import React from 'react';
import ReactDOM from 'react-dom';
import { shallow } from 'enzyme';
import { ListItem, ListItemProps, listItemNoDataMessage } from '../ListItem/ListItem';

describe('<ListItem />', () => {

    let div: any;
    let props: ListItemProps;

    beforeEach(() => {
        div = document.createElement('div');
        props = {
            item: {
                id: 0,
                title: 'Test Title',
                description: 'Test Description'
            },
        };
    });

    it('[SMOKE - DEEP] renders without crashing', () => {
        ReactDOM.render(<ListItem {...props} />, div);
        ReactDOM.unmountComponentAtNode(div);
    });

    it('[SMOKE - SHALLOW] renders without crashing', () => {
        shallow(<ListItem {...props} />);
    });

    describe('The correct data is displayed', () => {

        let component;

        beforeEach(() => {
            props = {
                item: {
                    id: 0,
                    title: 'Test Title',
                    description: 'Test Description'
                },
            };
        });

        it('should display the title', () => {
            component = shallow(<ListItem {...props} />);
            const heading = component.find(`[data-test="ListItemHeading"]`);
            expect(heading.length).toBe(1);
            expect(heading.text()).toBe(props.item.title);
        });

        it('should display the appropriate message if there is no title', () => {
            props.item.title = '';
            component = shallow(<ListItem {...props} />);
            const heading = component.find(`[data-test="ListItemHeading"]`);
            expect(heading.length).toBe(1);
            expect(heading.text()).toBe(listItemNoDataMessage);
        });

        it('should display the subtitle', () => {
            component = shallow(<ListItem {...props} />);
            const subheading = component.find(`[data-test="ListItemSubHeading"]`);
            expect(subheading.length).toBe(1);
            expect(subheading.text()).toBe(props.item.description);
        });

        it('should display the appropriate text if there is no subtitle', () => {
            props.item.description = '';
            component = shallow(<ListItem {...props} />);
            const subheading = component.find(`[data-test="ListItemSubHeading"]`);
            expect(subheading.length).toBe(1);
            expect(subheading.text()).toBe(listItemNoDataMessage);
        });

    });

});